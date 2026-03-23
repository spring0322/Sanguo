using Microsoft.Xna.Framework;
using System;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;


namespace GameObjects
{
    [DataContract]
    public partial class Map : System.Text.Json.Serialization.IJsonOnDeserialized
    {
        [DataMember]
        public Point JumpPosition;

        private int[,] mapData;
        private Point mapDimensions;
        private int tileWidth = 100;
        private int tileHeight = 100;
        private string dituwenjian;
        private int kuaishu = 20;
        private int meikuaidexiaokuaishu = 10;
        private bool useSimpleArchImages = false;

        [DataMember]
        public int TileWidthMin = 30;
        [DataMember]
        public int TileWidthMax = 100;

        [DataMember]
        public string MapName
        {
            get
            {
                if (String.IsNullOrEmpty(dituwenjian))
                {
                    return null;
                } 
                else 
                {
                    return dituwenjian;
                }
            }
            set
            {
                System.Diagnostics.Debug.WriteLine($"[Map] MapName setter called with: {value ?? "null"}");
                if (string.IsNullOrEmpty(value))
                {
                    dituwenjian = value;
                    return;
                }
                
                if (value.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || 
                    value.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || 
                    value.EndsWith(".dds", StringComparison.OrdinalIgnoreCase))
                {
                    // 修复：对于包含版本号的文件名（如_yueluo_1.0.jpg），只移除最后一个扩展名
                    int lastDotIndex = value.LastIndexOf('.');
                    dituwenjian = value.Substring(0, lastDotIndex);
                }
                else
                {
                    dituwenjian = value;
                }
                System.Diagnostics.Debug.WriteLine($"[Map] MapName processed to: {dituwenjian}");
            }
        }

        public void Init()
        {
            if (TileWidthMin == 0)
            {
                TileWidthMin = 30;
            }
            if (TileWidthMax == 0)
            {
                TileWidthMax = 100;
            }
        }

        public void Clear()
        {
            this.mapDimensions = Point.Zero;
            this.mapData = null;
        }

        public bool LoadMapData(string[] mapDataValueString, int X, int Y)
        {
            // Validate input parameters
            if (mapDataValueString == null)
            {
                System.Diagnostics.Debug.WriteLine("[Map] Error: Map data array is null.");
                return false;
            }
            
            if (X <= 0 || Y <= 0)
            {
                System.Diagnostics.Debug.WriteLine($"[Map] Error: Invalid map dimensions {X}x{Y}.");
                return false;
            }
            
            this.mapDimensions = new Point(X, Y);
            if (mapDataValueString.Length != this.MapTileCount)
            {
                System.Diagnostics.Debug.WriteLine($"[Map] Error: Map data count {mapDataValueString.Length} != {this.MapTileCount}.");
                return false;
            }
            this.mapData = new int[X, Y];
            for (int i = 0; i < this.MapTileCount; i++)
            {
                try
                {
                    this.mapData[i % X, i / X] = int.Parse(mapDataValueString[i]);
                }
                catch (Exception exception)
                {
                    System.Diagnostics.Debug.WriteLine($"[Map] Error parsing map data at index {i}: {exception.Message}");
                    return false;
                }
            }
            return true;
        }

        public bool LoadMapData(string mapdata, int X, int Y)
        {
            // Validate input parameters
            if (string.IsNullOrEmpty(mapdata))
            {
                System.Diagnostics.Debug.WriteLine("[Map] Warning: Map data is null or empty. Skipping LoadMapData.");
                return false;
            }
            
            if (X <= 0 || Y <= 0)
            {
                System.Diagnostics.Debug.WriteLine($"[Map] Error: Invalid map dimensions {X}x{Y}.");
                return false;
            }
            
            this.mapDimensions = new Point(X, Y);
            char[] separator = new char[] { ' ', '\n', '\r', '\t' };
            string[] strArray = mapdata.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            if (strArray.Length != this.MapTileCount)
            {
                System.Diagnostics.Debug.WriteLine($"[Map] Error: Splitted map data count {strArray.Length} != {this.MapTileCount}.");
                return false;
            }
            this.mapData = new int[X, Y];
            for (int i = 0; i < this.MapTileCount; i++)
            {
                try
                {
                    this.mapData[i % X, i / X] = int.Parse(strArray[i]);
                }
                catch (Exception exception)
                {
                    System.Diagnostics.Debug.WriteLine($"[Map] Error parsing map string data at index {i}: {exception.Message}");
                    return false;
                }
            }
            return true;
        }

        public bool LoadMapDataFromDataBase(string connectionString)
        {
            return true;
        }

        public bool PositionOutOfRange(Point mapPosition)
        {
            return ((((mapPosition.X < 0) || (mapPosition.Y < 0)) || (mapPosition.X >= this.mapDimensions.X)) || (mapPosition.Y >= this.mapDimensions.Y));
        }

        public void Replace(int terrainID1, int terrainID2)
        {
            for (int i = 0; i < this.mapDimensions.Y; i++)
            {
                for (int j = 0; j < this.mapDimensions.X; j++)
                {
                    if (this.mapData[j, i] == terrainID1)
                    {
                        this.mapData[j, i] = terrainID2;
                    }
                }
            }
        }

        public string SaveToString()
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < this.mapDimensions.Y; i++)
            {
                for (int j = 0; j < this.mapDimensions.X; j++)
                {
                    builder.Append(this.mapData[j, i].ToString() + " ");
                }
            }
            return builder.ToString();
        }
        
        public int[,] MapData
        {
            get
            {
                return this.mapData;
            }
            set
            {
                this.mapData = value;
            }
        }

        [DataMember]
        public string MapDataString { get; set; }

        [DataMember]
        public Point MapDimensions
        {
            get
            {
                return this.mapDimensions;
            }
            set
            {
                this.mapDimensions = value;
            }
        }

        public int MapTileCount
        {
            get
            {
                return (this.mapDimensions.X * this.mapDimensions.Y);
            }
        }
        [DataMember]
        public int TileHeight
        {
            get
            {
                return this.tileHeight;
            }
            set
            {
                this.tileHeight = value;
            }
        }
        [DataMember]
        public int TileWidth
        {
            get
            {
                return this.tileWidth;
            }
            set
            {
                this.tileWidth = value;
            }
        }

        public int TotalTileHeight
        {
            get
            {
                return (this.TileHeight * this.mapDimensions.Y);
            }
        }

        public int TotalTileWidth
        {
            get
            {
                return (this.TileWidth * this.mapDimensions.X);
            }
        }
        [DataMember]
        public int NumberOfTiles
        {
            get
            {
                return kuaishu;
            }
            set
            {
                kuaishu = value;
            }
        }
        [DataMember]
        public int NumberOfSquaresInEachTile
        {
            get
            {
                return meikuaidexiaokuaishu;
            }
            set
            {
                meikuaidexiaokuaishu = value;
            }
        }
        [DataMember]
        public bool UseSimpleArchImages
        {
            get
            {
                return useSimpleArchImages;
            }
            set
            {
                useSimpleArchImages = value;
            }
        }
        
        /// <summary>
        /// 反序列化后自动调用，从 MapDataString 重建 MapData
        /// </summary>
        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            RebuildMapData();
        }

        /// <summary>
        /// IJsonOnDeserialized 接口实现 —— STJ AOT 模式下的反序列化回调。
        /// [OnDeserialized] 特性仅被 DataContractSerializer 识别，STJ AOT 不调用它，
        /// 必须通过此接口触发，否则 mapData 永远为 null，导致 IndexOutOfRangeException。
        /// </summary>
        void System.Text.Json.Serialization.IJsonOnDeserialized.OnDeserialized()
        {
            RebuildMapData();
        }

        private void RebuildMapData()
        {
            System.Diagnostics.Debug.WriteLine($"[Map.OnDeserialized] 开始重建地图数据");
            System.Diagnostics.Debug.WriteLine($"  - MapDataString 长度: {MapDataString?.Length ?? 0}");
            System.Diagnostics.Debug.WriteLine($"  - MapDimensions: {mapDimensions.X}x{mapDimensions.Y}");
            
            // 🔥 关键修复：从 MapDataString 重建 MapData
            if (!string.IsNullOrEmpty(MapDataString) && mapDimensions.X > 0 && mapDimensions.Y > 0)
            {
                bool success = LoadMapData(MapDataString, mapDimensions.X, mapDimensions.Y);
                if (success)
                {
                    System.Diagnostics.Debug.WriteLine($"[Map.OnDeserialized] ✅ 地图数据重建成功");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[Map.OnDeserialized] ❌ 地图数据重建失败");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[Map.OnDeserialized] ⚠️ MapDataString 为空或地图尺寸无效，无法重建地图数据");
            }
        }
    }
}

