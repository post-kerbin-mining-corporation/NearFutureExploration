using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using KSP.Localization;

namespace NearFutureExploration
{
  public class ModuleDeployableReflector : ModuleDeployablePart
  {
    [KSPField(isPersistant = false)]
    public double AddedRange = 1000000d;

    [KSPField(isPersistant = false)]
    public string DishColliderName;

    // Info for ui
    public override string GetModuleDisplayName()
    {
      return Localizer.Format("#LOC_NFEX_ModuleDeployableReflector_ModuleName");
    }

    public override string GetInfo()
    {
      return Localizer.Format("#LOC_NFEX_ModuleDeployableReflector_PartInfo", Utils.ToSI(AddedRange,"F0"));
    }

    public List<Collider> dishColliders;
    
    public override void OnStart(StartState state)
    {
      base.OnStart(state);

      dishColliders = new List<Collider>();
      if (DishColliderName != "")
      {
        try
        {
          Transform[] dishes = part.FindModelTransforms(DishColliderName);
          foreach (Transform dish in dishes)
          {
            dishColliders.Add(dish.GetComponent<Collider>());
          }
        }
        catch
        {
          Debug.LogError(String.Format("[NearFutureExploration]: [ModuleDeployableReflector]: No collider named {0} was found!", DishColliderName));
        }
      }
      else
      {
        Debug.LogError(String.Format("[NearFutureExploration]: [ModuleDeployableReflector]: DishColliderName must be specified!"));
      }
    }
    public double GetReflectorBonus()
    {
      if (deployState == DeployState.EXTENDED)
      {
        return AddedRange;
      } else
      {
        return 0d;
      }
    }
  }
}
