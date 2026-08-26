using Assets.Scripts.Entities.Player.Character;
using Assets.Scripts.Entities.Player.Turret;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Assets.Scripts.Entities.Game
{
  public partial struct DefeatLogSystem : ISystem
  {
    public void OnUpdate(ref SystemState state)
    {
      var ecb = new EntityCommandBuffer(Allocator.Temp);

      foreach (var (_, entity) in SystemAPI.Query<RefRO<PlayerDefeated>>()
        .WithNone<DefeatLogged>()
        .WithEntityAccess())
      {
        Debug.Log("Player defeated.");
        ecb.AddComponent<DefeatLogged>(entity);
      }

      foreach (var (_, entity) in SystemAPI.Query<RefRO<TurretDefeated>>()
        .WithNone<DefeatLogged>()
        .WithEntityAccess())
      {
        Debug.Log("Turret defeated.");
        ecb.AddComponent<DefeatLogged>(entity);
      }

      ecb.Playback(state.EntityManager);
      ecb.Dispose();
    }
  }

  public struct DefeatLogged : IComponentData
  {
  }
}
