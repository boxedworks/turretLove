using System.Collections.Generic;
using System.IO;
using Assets.Scripts.Entities.Enemy;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Unity.Entities;
using UnityEngine;

namespace Assets.Scripts.Entities.Loot.DropTables
{
  public partial struct DropTableSystem : ISystem
  {
    // Stored as a string-keyed dictionary for human-readable JSON
    private static readonly JsonSerializerSettings s_JsonSettings = new()
    {
      Formatting = Formatting.Indented,
      Converters = { new StringEnumConverter() }
    };

    private static string FilePath => Path.Combine(Application.dataPath, "..", "drop_tables.json");

    static Dictionary<EnemyType, EnemyDropTable> DropTables;

    public void OnCreate(ref SystemState state)
    {
      DropTables = Load();
    }

    public static void Save()
    {
      var json = JsonConvert.SerializeObject(DropTables, s_JsonSettings);
      File.WriteAllText(FilePath, json);
    }

    public static Dictionary<EnemyType, EnemyDropTable> Load()
    {
      if (!File.Exists(FilePath))
        return new Dictionary<EnemyType, EnemyDropTable>();

      var json = File.ReadAllText(FilePath);
      return JsonConvert.DeserializeObject<Dictionary<EnemyType, EnemyDropTable>>(json, s_JsonSettings)
             ?? new Dictionary<EnemyType, EnemyDropTable>();
    }

    public static Dictionary<LootType, int> RollDrops(EnemyType enemyType)
    {
      var results = new Dictionary<LootType, int>();

      if (!DropTables.TryGetValue(enemyType, out var enemyDropTable))
        return results;

      var rng = new System.Random();

      foreach (var dropTable in enemyDropTable.DropTables)
      {
        var rolls = dropTable.Rolls;
        for (var i = 0; i < rolls; i++)
        {
          var totalWeight = 0.0;
          foreach (var entry in dropTable.Entries)
            totalWeight += entry.DropChance;

          var roll = rng.NextDouble() * totalWeight;
          var cumulative = 0.0;
          foreach (var entry in dropTable.Entries)
          {
            cumulative += entry.DropChance;
            if (roll >= cumulative)
              continue;

            var amount = rng.Next(entry.MinAmount, entry.MaxAmount + 1);
            results[entry.Type] = results.TryGetValue(entry.Type, out var existing)
              ? existing + amount
              : amount;
            break;
          }
        }
      }

      return results;
    }
  }
}
