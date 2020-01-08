using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NearFutureExploration
{
  public class ModuleForceSkinnedMeshLOD: PartModule
  {

    [KSPField(isPersistant = false)]
    public string SkinnedMeshName;


    [KSPField(isPersistant = false)]
    public int ForcedDetailLevel;

    public override void OnStart(StartState state)
    {
      base.OnStart(state);
      Transform[] xForms = part.FindModelTransforms(SkinnedMeshName);

      foreach (Transform t in xForms)
      {
        SkinnedMeshRenderer smr = t.GetComponent<SkinnedMeshRenderer>();
        if (!smr)
        {
          Debug.LogError(String.Format("[NearFutureExploration]: [ModuleForceSkinnedMeshLOD]: Could not find any SMRs named {0}", SkinnedMeshName));

        } else
        {
          smr.quality = (SkinQuality)(int)(Mathf.Clamp(ForcedDetailLevel, 0, 3));
        }
      }
    }
  }
}
