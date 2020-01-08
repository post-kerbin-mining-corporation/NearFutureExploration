using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Concept by gotmachine
/// </summary>
namespace NearFutureExploration
{
  public class ModuleDataTransmitterFeedeable : ModuleDataTransmitter, ICommAntenna
  {
    // This field allows the buffed antenna power to be saved
    [KSPField(isPersistant = true)]
    public double savedAntennaPower;

    public override void OnAwake()
    {
      savedAntennaPower = antennaPower;
      base.OnAwake();
    }

    // Not virtual so need a delete. 
    new public double CommPowerUnloaded(ProtoPartModuleSnapshot protoShot)
    {
      // pull savedAntennaPower if you can, if not, fall back to the original method
      if (protoShot != null && protoShot.moduleValues.TryGetValue("savedAntennaPower", ref savedAntennaPower))
      {
        return savedAntennaPower;
      }
      else
      {
        // Base version
        return base.CommPowerUnloaded(protoShot);
      }
    }
  }
}
