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
    public readonly struct DropResult
    {
      public readonly LootType LootType;
      public readonly int Amount;

      public DropResult(LootType lootType, int amount)
      {
        LootType = lootType;
        Amount = amount;
      }
    }

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

    public static List<DropResult> RollDrops(EnemyType enemyType)
    {
      var results = new List<DropResult>();

      if (!DropTables.TryGetValue(enemyType, out var enemyDropTable))
        return results;

      var rng = new System.Random();

      foreach (var dropTable in enemyDropTable.DropTables)
        RollDropTable(dropTable, results, rng);

      return results;
    }

    private static void RollDropTable(
      DropTable dropTable,
      List<DropResult> results,
      System.Random rng)
    {
      var totalWeight = AddGuaranteedDrops(dropTable, results, rng);

      if (totalWeight <= 0f)
        return;

      var totalRolls = GetTotalRolls(dropTable.RollChance, rng);
      for (var i = 0; i < totalRolls; i++)
        RollWeightedDrop(dropTable.Entries, totalWeight, results, rng);
    }

    private static float AddGuaranteedDrops(
      DropTable dropTable,
      List<DropResult> results,
      System.Random rng)
    {
      var totalWeight = 0f;

      foreach (var entry in dropTable.Entries)
      {
        var guaranteedDrops = Mathf.FloorToInt(entry.DropChance / 100f);
        for (var i = 0; i < guaranteedDrops; i++)
          AddDrop(results, entry, rng);

        totalWeight += entry.DropChance % 100f;
      }

      return totalWeight;
    }

    private static int GetTotalRolls(float rollChance, System.Random rng)
    {
      var totalRolls = Mathf.FloorToInt(rollChance / 100f);
      var remainingRollChance = rollChance % 100f;

      if (rng.NextDouble() * 100f < remainingRollChance)
        totalRolls++;

      return totalRolls;
    }

    private static void RollWeightedDrop(
      DropEntry[] entries,
      float totalWeight,
      List<DropResult> results,
      System.Random rng)
    {
      var roll = rng.NextDouble() * totalWeight;
      var cumulative = 0f;

      foreach (var entry in entries)
      {
        cumulative += entry.DropChance % 100f;
        if (roll >= cumulative)
          continue;

        AddDrop(results, entry, rng);
        break;
      }
    }

    private static void AddDrop(
      List<DropResult> results,
      DropEntry entry,
      System.Random rng)
    {
      var amount = rng.Next(entry.MinAmount, entry.MaxAmount + 1);
      if (entry.Type != LootType.None)
      {
        for (var index = 0; index < results.Count; index++)
        {
          if (results[index].LootType != entry.Type)
            continue;
          var existing = results[index];
          results[index] = new DropResult(existing.LootType, existing.Amount + amount);
          return;
        }
      }
      results.Add(new DropResult(entry.Type, amount));
    }
  }
}
