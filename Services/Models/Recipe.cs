using loggerApp.Queue;
using loggerMigrator.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace loggerApp.Models
{
    public class Recipe :  EntityBase, IQueueingData
    {
        [NotMapped]
        public string Name { get; set; }

        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int RecipeId { get; set; }      // ClassName + "Id" で PK

        public DateTime WroteTime { get; set; }

        public Int16 Count { get; set; }       // UInt16 だと、対象にならないので、最大まで必要そうなら、Int32に変更必須
        [MaxLength(256)]
        public string DatFileName { get; set; }

        public short WaferNo { get; set; }      // 共通にしないと使いづらいので、内部でShort変換する
        [MaxLength(256)]
        public string CassetteId { get; set; }  // FoupId は他用途での名称。Formatをそのまま流用した為らしい
        [MaxLength(256)]
        public string LotNo { get; set; }
        [MaxLength(256)]
        public string WaferId { get; set; }
        [MaxLength(256)]
        public string DeviceNumber { get; set; }
        [MaxLength(256)]
        public string DeviceVersion { get; set; }
        [MaxLength(256)]
        public string LayerNumber { get; set; }
        [MaxLength(256)]
        public string ProductName { get; set; }
        [MaxLength(256)]
        public string IntegratedRecipe { get; set; }
        [MaxLength(256)]
        public string MapName { get; set; }
        [MaxLength(256)]
        public string ModuleStatus { get; set; }
        [MaxLength(256)]
        public string ProcessRecipe { get; set; }

        public Recipe()
        {
            // Not Null 項目に対する初期化。
            WroteTime = System.Data.SqlTypes.SqlDateTime.MinValue.Value;

        }
    }
}
