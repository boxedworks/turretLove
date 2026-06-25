

using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace Assets.Scripts.Entities.Game
{
  public partial struct EffectSystem : ISystem
  {

    [BurstCompile]
    partial struct BlinkJob : IJobEntity
    {

      public EntityCommandBuffer Ecb;
      public float ElapsedTime;

      public readonly void Execute(Entity entity, ref ColorOverride colorOverride, ref BlinkEffect blinkEffect)
      {
        // Toggle color based on rate
        if (blinkEffect.LastBlinkTime + blinkEffect.Rate < ElapsedTime)
        {
          blinkEffect.LastBlinkTime = ElapsedTime;
          blinkEffect.BlinkCount--;
          blinkEffect.Toggle = !blinkEffect.Toggle;

          if (blinkEffect.Toggle)
            colorOverride.Value = blinkEffect.BlinkColor;
          else
          {
            colorOverride.Value = new float4(1f, 1f, 1f, 1f);

            // Remove BlinkEffect component when done
            if (blinkEffect.BlinkCount <= 0)
              Ecb.RemoveComponent<BlinkEffect>(entity);
          }
        }

      }
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
      state.Dependency = new BlinkJob()
      {
        Ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
        ElapsedTime = (float)SystemAPI.Time.ElapsedTime
      }
        .Schedule(state.Dependency);
    }
  }

  //
  [MaterialProperty("_Color")]
  public struct ColorOverride : IComponentData
  {
    public float4 Value;
  }

  //
  public struct BlinkEffect : IComponentData
  {
    public float Rate;
    public double LastBlinkTime;
    public int BlinkCount;
    public bool Toggle;
    public float4 BlinkColor;
  }
}