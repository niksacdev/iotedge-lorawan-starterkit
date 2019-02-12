// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace LoRaWan.NetworkServer
{
    using System;
    using System.Globalization;
    using System.Net.Http;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using System.Web;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// LoRa payload decoder
    /// </summary>
    public class LoRaPayloadDecoder : ILoRaPayloadDecoder
    {
        // Http client used by decoders
        // Decoder calls don't need proxy since they will never leave the IoT Edge device
        Lazy<HttpClient> decodersHttpClient = new Lazy<HttpClient>(() =>
        {
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Connection", "Keep-Alive");
            client.DefaultRequestHeaders.Add("Keep-Alive", "timeout=86400");
            return client;
        });

        public async ValueTask<DecodePayloadResult> DecodeMessageAsync(string devEUI, byte[] payload, byte fport, string sensorDecoder)
        {
            sensorDecoder = sensorDecoder ?? string.Empty;

            DecodePayloadResult result;
            var base64Payload = Convert.ToBase64String(payload);

            // Call local decoder (no "http://" in SensorDecoder)
            if (!sensorDecoder.Contains("http://"))
            {
                Type decoderType = typeof(LoRaPayloadDecoder);
                MethodInfo toInvoke = decoderType.GetMethod(sensorDecoder, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);

                if (toInvoke != null)
                {
                    result = (DecodePayloadResult)toInvoke.Invoke(null, new object[] { devEUI, payload, fport });
                }
                else
                {
                    result = new DecodePayloadResult()
                    {
                        Error = "No '{sensorDecoder}' decoder found",
                        RawPayload = base64Payload,
                    };
                }
            }
            else
            {
                // Call SensorDecoderModule hosted in seperate container ("http://" in SensorDecoder)
                // Format: http://containername/api/decodername
                string toCall = sensorDecoder;

                if (sensorDecoder.EndsWith("/"))
                {
                    toCall = sensorDecoder.Substring(0, sensorDecoder.Length - 1);
                }

                // use HttpUtility to UrlEncode Fport and payload
                var payloadEncoded = HttpUtility.UrlEncode(base64Payload);
                var devEUIEncoded = HttpUtility.UrlEncode(devEUI);

                // Add Fport and Payload to URL
                toCall = $"{toCall}?devEUI={devEUIEncoded}&fport={fport.ToString()}&payload={payloadEncoded}";

                // Call SensorDecoderModule
                result = await this.CallSensorDecoderModule(toCall, payload);
            }

            return result;
        }

        async Task<DecodePayloadResult> CallSensorDecoderModule(string sensorDecoderModuleUrl, byte[] payload)
        {
            var base64Payload = Convert.ToBase64String(payload);

            try
            {
                HttpResponseMessage response = await this.decodersHttpClient.Value.GetAsync(sensorDecoderModuleUrl);

                if (!response.IsSuccessStatusCode)
                {
                    var badReqResult = await response.Content.ReadAsStringAsync();

                    return new DecodePayloadResult(new
                    {
                        error = $"SensorDecoderModule '{sensorDecoderModuleUrl}' returned bad request.",
                        exceptionMessage = badReqResult ?? string.Empty,
                        rawpayload = base64Payload
                    });
                }
                else
                {
                    return JsonConvert.DeserializeObject<DecodePayloadResult>(await response.Content.ReadAsStringAsync());
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Error in decoder handling: {ex.Message}", LogLevel.Error);

                return new DecodePayloadResult(new
                {
                    error = $"Call to SensorDecoderModule '{sensorDecoderModuleUrl}' failed.",
                    exceptionMessage = ex.Message,
                    rawpayload = base64Payload
                });
            }
        }

        /// <summary>
        /// Value sensor decoding, from <see cref="byte[]"/> to <see cref="DecodePayloadResult"/>
        /// </summary>
        /// <param name="devEUI">Device identifier</param>
        /// <param name="payload">The payload to decode</param>
        /// <param name="fport">The received frame port</param>
        /// <returns>The decoded value as a JSON string</returns>
        public static DecodePayloadResult DecoderValueSensor(string devEUI, byte[] payload, uint fport)
        {
            var result = Encoding.UTF8.GetString(payload);

            if (long.TryParse(result, NumberStyles.Float, CultureInfo.InvariantCulture, out var longValue))
            {
                return new DecodePayloadResult(longValue);
            }

            if (double.TryParse(result, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleValue))
            {
                return new DecodePayloadResult(doubleValue);
            }

            return new DecodePayloadResult(result);
        }
    }
}