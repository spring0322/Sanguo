using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework;
using Platforms;
using WorldOfTheThreeKingdoms.Serialization;
using GameObjects;
using GameManager;

namespace Tools
{
    /// <summary>
    /// System.Text.Json鐗堟湰鐨勫簭鍒楀寲鍣?
    /// 鏇挎崲SimpleSerializer涓殑Newtonsoft.Json鍔熻兘锛屾彁渚汚OT鍏煎鎬?
    /// </summary>
    public class SimpleSerializerSystemTextJson
    {
        private static readonly JsonSerializerOptions _defaultOptions;
        private static readonly JsonSerializerOptions _indentedOptions;

        static SimpleSerializerSystemTextJson()
        {
            _defaultOptions = CreateJsonOptions(false);
            _indentedOptions = CreateJsonOptions(true);
        }

        /// <summary>
        /// 鍒涘缓JSON搴忓垪鍖栭€夐」
        /// </summary>
        /// <param name="indented">鏄惁鏍煎紡鍖栬緭鍑?/param>
        /// <returns>JSON搴忓垪鍖栭€夐」</returns>
        private static JsonSerializerOptions CreateJsonOptions(bool indented)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = indented,
                PropertyNamingPolicy = null, // 淇濇寔鍘熷灞炴€у悕
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                ReferenceHandler = ReferenceHandler.Preserve, // 澶勭悊寰幆寮曠敤
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                TypeInfoResolver = GameJsonContext.Default,
                Converters =
                {
                    new WorldOfTheThreeKingdoms.Serialization.SystemTextJson.GameObjectListConverter(),
                    // new WorldOfTheThreeKingdoms.Serialization.SystemTextJson.LegacyDictionaryConverter(),
                    new WorldOfTheThreeKingdoms.Serialization.SystemTextJson.GameObjectReferenceConverter(),
                    new WorldOfTheThreeKingdoms.Serialization.SystemTextJson.FactionLeaderConverter(),
                    new WorldOfTheThreeKingdoms.Serialization.SystemTextJson.PersonIdealTendencyConverter(),
                    new WorldOfTheThreeKingdoms.Serialization.SystemTextJson.IdealTendencyKindConverter()
                }
            };

            return options;
        }

        private static JsonTypeInfo<T> ResolveTypeInfo<T>(JsonSerializerOptions options)
        {
            JsonTypeInfo typeInfo = options.GetTypeInfo(typeof(T));
            if (typeInfo is not JsonTypeInfo<T> typedTypeInfo)
            {
                throw new InvalidOperationException($"AOT metadata not registered for type: {typeof(T).FullName}");
            }

            return typedTypeInfo;
        }

        #region XMLSerializer (淇濇寔涓嶅彉)
        [RequiresUnreferencedCode("XML serialization is not trim-safe. This legacy path must not be used in NativeAOT runtime flows.")]
        [RequiresDynamicCode("XML serialization may require runtime code generation and is not NativeAOT-safe.")]
        public static string SerializeXML<T>(T t)
        {
            using (StringWriter sw = new StringWriter())
            {
                lock (Platform.SerializerLock)
                {
                    Platform.SessionActive = false;
                    XmlSerializer xz = new XmlSerializer(t.GetType());
                    xz.Serialize(sw, t);
                    Platform.SessionActive = true;
                }
                return sw.ToString();
            }
        }

        [RequiresUnreferencedCode("XML deserialization is not trim-safe. This legacy path must not be used in NativeAOT runtime flows.")]
        [RequiresDynamicCode("XML deserialization may require runtime code generation and is not NativeAOT-safe.")]
        public static T DeserializeXML<T>(string s)
        {
            using (StringReader sr = new StringReader(s))
            {
                XmlSerializer xz = new XmlSerializer(typeof(T));
                try
                {
                    lock (Platform.SerializerLock)
                    {
                        Platform.SessionActive = false;
                        T t = (T)xz.Deserialize(sr);
                        Platform.SessionActive = true;
                        return t;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SimpleSerializerSystemTextJson] DeserializeXML failed for type {typeof(T).Name}: {ex.Message}");
                    throw;
                }
            }
        }
        #endregion

        #region JsonSerializer (System.Text.Json鐗堟湰)

        /// <summary>
        /// 浣跨敤System.Text.Json搴忓垪鍖栧璞′负JSON瀛楃涓?
        /// </summary>
        /// <param name="t">瑕佸簭鍒楀寲鐨勫璞?/param>
        /// <param name="zip">鏄惁GZip鍘嬬缉缁撴灉</param>
        /// <param name="indented">鏄惁鏍煎紡鍖栬緭鍑?/param>
        /// <param name="net">閬楃暀鍙傛暟锛堝吋瀹规€э級</param>
        public static string SerializeJson<T>(T t, bool zip = false, bool indented = false, bool net = false)
        {
            string result = null;
            lock (Platform.SerializerLock)
            {
                try
                {
                    var options = indented ? _indentedOptions : _defaultOptions;
                    result = JsonSerializer.Serialize(t, ResolveTypeInfo<T>(options));
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SimpleSerializerSystemTextJson] SerializeJson failed for type {typeof(T).Name}: {ex.Message}");
                    throw;
                }
            }
            
            if (zip)
            {
                result = result.GZipCompressString();
            }
            return result;
        }

        /// <summary>
        /// 浣跨敤System.Text.Json鍙嶅簭鍒楀寲JSON瀛楃涓蹭负瀵硅薄
        /// 馃敟 淇敼锛氬湪鍙嶅簭鍒楀寲鍚庤嚜鍔ㄦ墽琛屽紩鐢ㄤ慨澶?
        /// </summary>
        /// <param name="s">JSON瀛楃涓?/param>
        /// <param name="zip">瀛楃涓叉槸鍚Zip鍘嬬缉</param>
        /// <param name="net">閬楃暀鍙傛暟锛堝吋瀹规€э級</param>
        public static T DeserializeJson<T>(string s, bool zip = false, bool net = false)
        {
            if (zip)
            {
                s = s.GZipDecompressString();
            }

            T t;
            try
            {
                lock (Platform.SerializerLock)
                {
                    t = JsonSerializer.Deserialize(s, ResolveTypeInfo<T>(_defaultOptions));
                    if (t == null)
                    {
                        throw new InvalidOperationException($"System.Text.Json 反序列化 {typeof(T).Name} 返回 null");
                    }
                    
                    // 馃敟銆愭牴鏈€т慨澶嶃€戰煍?
                    // 鍦ㄩ攣鍐呭鐞嗭紝纭繚绾跨▼瀹夊叏锛岀洿鎺ュ垏鏂笉璇ュ叡浜殑寮曠敤
                    FixSharedReferences(t);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SimpleSerializerSystemTextJson] DeserializeJson failed for type {typeof(T).Name}: {ex.Message}");
                throw;
            }

            return t;
        }

        /// <summary>
        /// 寮傛搴忓垪鍖栧璞′负JSON瀛楃涓?
        /// </summary>
        public static async Task<string> SerializeJsonAsync<T>(T t, bool zip = false, bool indented = false)
        {
            string result = null;
            
            try
            {
                var options = indented ? _indentedOptions : _defaultOptions;
                
                using (var stream = new MemoryStream())
                {
                    await JsonSerializer.SerializeAsync(stream, t, ResolveTypeInfo<T>(options)).ConfigureAwait(false);
                    result = Encoding.UTF8.GetString(stream.ToArray());
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SimpleSerializerSystemTextJson] SerializeJsonAsync failed for type {typeof(T).Name}: {ex.Message}");
                throw;
            }
            
            if (zip)
            {
                result = await Task.Run(() => result.GZipCompressString()).ConfigureAwait(false);
            }
            return result;
        }

        /// <summary>
        /// 寮傛鍙嶅簭鍒楀寲JSON瀛楃涓蹭负瀵硅薄
        /// 馃敟 淇敼锛氬湪鍙嶅簭鍒楀寲鍚庤嚜鍔ㄦ墽琛屽紩鐢ㄤ慨澶?
        /// </summary>
        public static async Task<T> DeserializeJsonAsync<T>(string s, bool zip = false)
        {
            if (zip)
            {
                s = await Task.Run(() => s.GZipDecompressString()).ConfigureAwait(false);
            }

            T t;
            try
            {
                using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(s)))
                {
                    t = await JsonSerializer.DeserializeAsync(stream, ResolveTypeInfo<T>(_defaultOptions)).ConfigureAwait(false);
                    if (t == null)
                    {
                        throw new InvalidOperationException($"System.Text.Json 异步反序列化 {typeof(T).Name} 返回 null");
                    }
                }
                
                // 馃敟銆愭牴鏈€т慨澶嶃€戰煍?
                // 纭繚寮傛鍔犺浇涔熻兘浜彈鍒颁慨澶?
                FixSharedReferences(t);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SimpleSerializerSystemTextJson] DeserializeJsonAsync failed for type {typeof(T).Name}: {ex.Message}");
                throw;
            }

            return t;
        }
        
        /// <summary>
        /// 馃敟 鏍稿績淇閫昏緫锛氬己鍒跺垏鏂?ArchitectureArea 鐨勫叡浜紩鐢?
        /// 姝ゆ柟娉曢€氳繃鍙嶅皠鎴栧姩鎬佺被鍨嬫鏌ワ紝纭繚涓嶄粎 GameScenario锛屽寘鍚?Architectures 鐨勪换浣曞璞￠兘鑳借淇
        /// </summary>
        private static void FixSharedReferences(object root)
        {
            if (root is not GameScenario scenario)
            {
                return;
            }

            try
            {
                var architectures = scenario.Architectures?.GetList();
                if (architectures == null)
                {
                    return;
                }

                for (int i = 0; i < architectures.Count; i++)
                {
                    if (architectures[i] is not Architecture architecture)
                    {
                        continue;
                    }

                    architecture.ArchitectureArea = new GameArea();
                    if (!string.IsNullOrEmpty(architecture.ArchitectureAreaString))
                    {
                        architecture.LoadFromString(architecture.ArchitectureArea, architecture.ArchitectureAreaString);
                    }
                }
            }
            catch (Exception ex)
            {
                // 浠呬粎璁板綍鏃ュ織锛屼笉闃绘柇娴佺▼銆傝繖鍙槸涓€涓ˉ涓侊紝涓嶅簲瀵艰嚧娓告垙宕╂簝銆?
                System.Diagnostics.Debug.WriteLine($"[FixSharedReferences] Warning: {ex.Message}");
            }
        }

        /// <summary>
        /// 楠岃瘉JSON搴忓垪鍖栧線杩斾竴鑷存€?
        /// </summary>
        /// <param name="obj">瑕佹祴璇曠殑瀵硅薄</param>
        /// <returns>寰€杩旀祴璇曟槸鍚︽垚鍔?/returns>
        public static bool ValidateSerializationRoundTrip<T>(T obj)
        {
            try
            {
                var json = SerializeJson(obj);
                var deserialized = DeserializeJson<T>(json);
                
                // 鍩烘湰楠岃瘉锛氱‘淇濆弽搴忓垪鍖栨垚鍔熶笖涓嶄负null
                if (deserialized == null && obj != null)
                {
                    return false;
                }
                
                if (deserialized != null && obj == null)
                {
                    return false;
                }
                
                // 瀵逛簬GameObject绫诲瀷锛岄獙璇両D鏄惁涓€鑷?
                if (obj is GameObject gameObj && deserialized is GameObject deserializedGameObj)
                {
                    return gameObj.ID == deserializedGameObj.ID;
                }
                
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SimpleSerializerSystemTextJson] Validation failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 鑾峰彇搴忓垪鍖栫粺璁′俊鎭?
        /// </summary>
        /// <returns>缁熻淇℃伅瀛楃涓?/returns>
        public static string GetSerializationStats()
        {
            return "System.Text.Json搴忓垪鍖栧櫒 - AOT鍏煎鐗堟湰 (鍚獹ameArea寮曠敤鐑慨澶?";
        }

        #endregion

        #region 鍏煎鎬ф柟娉?

        /// <summary>
        /// 鍏煎鎬ф柟娉曪細淇濆瓨瀵硅薄鍒版枃浠?
        /// </summary>
        public static void SaveToFile<T>(T obj, string filePath, bool indented = true)
        {
            try
            {
                var json = SerializeJson(obj, indented: indented);
                File.WriteAllText(filePath, json, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SimpleSerializerSystemTextJson] SaveToFile failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 寮傛淇濆瓨瀵硅薄鍒版枃浠?
        /// </summary>
        public static async Task SaveToFileAsync<T>(T obj, string filePath, bool indented = true, CancellationToken cancellationToken = default)
        {
            try
            {
                var json = await SerializeJsonAsync(obj, indented: indented).ConfigureAwait(false);
                await File.WriteAllTextAsync(filePath, json, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SimpleSerializerSystemTextJson] SaveToFileAsync failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 鍏煎鎬ф柟娉曪細浠庢枃浠跺姞杞藉璞?
        /// </summary>
        public static T LoadFromFile<T>(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"鏂囦欢涓嶅瓨鍦? {filePath}");
                }
                
                var json = File.ReadAllText(filePath, Encoding.UTF8);
                return DeserializeJson<T>(json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SimpleSerializerSystemTextJson] LoadFromFile failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 寮傛浠庢枃浠跺姞杞藉璞?
        /// </summary>
        public static async Task<T> LoadFromFileAsync<T>(string filePath, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"鏂囦欢涓嶅瓨鍦? {filePath}");
                }
                
                var json = await File.ReadAllTextAsync(filePath, Encoding.UTF8, cancellationToken).ConfigureAwait(false);
                return await DeserializeJsonAsync<T>(json).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SimpleSerializerSystemTextJson] LoadFromFileAsync failed: {ex.Message}");
                throw;
            }
        }

        #endregion
    }
}
