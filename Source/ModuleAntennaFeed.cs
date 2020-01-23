using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.Localization;
using VisualDebugUtils;

namespace NearFutureExploration
{
  public class ModuleAntennaFeed:PartModule
  {

    [KSPField(isPersistant = false)]
    public string FeedOffset;

    [KSPField(isPersistant = false)]
    public string FeedVector;

    [KSPField(isPersistant = false)]
    public string FeedTransformName;

    [KSPField(isPersistant = false)]
    public float FeedScale = 1.0f;

    [KSPField(isPersistant = false)]
    public float RayDistance = 500f;

    [KSPField(isPersistant = false, guiActive = true, guiName = "Reflector Buff")]
    public string StatusString;

    [KSPField(isPersistant = false, guiActive = true, guiName = "Using Reflector")]
    public string TargetString;

    // Toggle Visibility
    [KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "Render Antenna Path")]
    public void ToggleVisibility()
    {
      lineRenderable = !lineRenderable;
      renderedLine.SetVisibility(lineRenderable);
    }

    // Toggle Visibility All
    [KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "Render All Paths")]
    public void ToggleVisibilityAll()
    {
      bool targetState = false;
      if (!lineRenderable)
        targetState = true;
      foreach (Part p in EditorLogic.fetch.ship.Parts)
      {
        ModuleAntennaFeed feed = p.GetComponent<ModuleAntennaFeed>();
        if (feed)
        {
          feed.SetVisibility(targetState);
        }
      }
    }

    public void SetVisibility(bool visibility)
    {
      lineRenderable = visibility;
      renderedLine.SetVisibility(visibility);
    }


    Color badColor;
    Color goodColor;

    Vector3 feedOffset = Vector3.zero;
    Vector3 feedVector = Vector3.zero;

    Transform feedTransform;
    bool deployable = false;
    public double baseAntennaRange;

    ModuleDeployableReflector reflector;
    ModuleDataTransmitterFeedeable baseAntenna;
    ModuleDeployableAntenna deployModule;

    bool lineRenderable = true;
    DebugLine renderedLine;

    public ModuleDataTransmitterFeedeable Antenna { get { return baseAntenna; } }

    // Info for ui
    public override string GetInfo()
    {
      return Localizer.Format("#LOC_NFEX_ModuleAntennaFeed_PartInfo", (FeedScale*100.0).ToString("F0"));
    }

    public override string GetModuleDisplayName()
    {
      return Localizer.Format("#LOC_NFEX_ModuleAntennaFeed_ModuleName");
    }


    Vector3 RaycastStartPoint
    {
      get {
        if (feedTransform != null)
          return feedTransform.position;
        else
          return part.partTransform.TransformPoint( feedOffset);
      }
    }
    Vector3 RaycastDirection
    {
      get
      {
        if (feedTransform != null)
          return feedTransform.forward;
        else
          return part.partTransform.TransformDirection(feedVector);
      }
    }
    public void Start()
    {
      feedOffset = ConfigNode.ParseVector3(FeedOffset);
      feedVector = ConfigNode.ParseVector3(FeedVector);
      Localize();
      FindAntenna();
      SetupFeedPoint();
      SetupRenderer();
    }
    void Localize()
    {
      Events["ToggleVisibility"].guiName = Localizer.Format("#LOC_NFEX_ModuleAntennaFeed_Event_ShowPath_Title");
      Fields["StatusString"].guiName = Localizer.Format("#LOC_NFEX_ModuleAntennaFeed_Field_ReflectorBuff_Title");
      Fields["TargetString"].guiName = Localizer.Format("#LOC_NFEX_ModuleAntennaFeed_Field_ReflectorName_Title");
    }

    public override void OnWillBeCopied(bool asSymCounterpart)
    {
      base.OnWillBeCopied(asSymCounterpart);
      renderedLine.SetVisibility(false);
    }
    public override void OnWasCopied(PartModule copyPartModule, bool asSymCounterpart)
    {
      base.OnWasCopied(copyPartModule, asSymCounterpart);
      renderedLine.SetVisibility(lineRenderable);

    }
    int ticker = 0;

    void FixedUpdate()
    {
      ticker++;
      if (baseAntenna != null && ticker >= 5)
      {
        ticker = 0;
        if (!deployable || (deployable && deployModule.deployState == ModuleDeployablePart.DeployState.EXTENDED))
        {
          if (TestLOSAll(out reflector))
          {
            ApplyReflectorBonus();

            Fields["StatusString"].guiActive = true;
            Fields["TargetString"].guiActive = true;

            Fields["StatusString"].guiActiveEditor = true;
            Fields["TargetString"].guiActiveEditor = true;
          } else
          {
            NullReflectorBonus();

            Fields["StatusString"].guiActive = false;
            Fields["TargetString"].guiActive = false;

            Fields["StatusString"].guiActiveEditor = false;
            Fields["TargetString"].guiActiveEditor = false;
          }

        }
        else
        {
          if ((deployable && deployModule.deployState != ModuleDeployablePart.DeployState.EXTENDED))
          {
            NullReflectorBonus();
            Fields["StatusString"].guiActive = false;
            Fields["TargetString"].guiActive = false;

            Fields["StatusString"].guiActiveEditor = false;
            Fields["TargetString"].guiActiveEditor = false;
          }
          if (lineRenderable)
            renderedLine.SetVisibility(true);
          renderedLine.AdjustSize(30f);
          renderedLine.SetColor(badColor);
        }
      }
    }
    public void OnDestroy()
    {
      if (renderedLine != null)
        renderedLine.Destroy();
    }

    void SetupFeedPoint()
    {
      if (FeedTransformName != "")
      {
        Debug.Log("[NearFutureExploration] [ModuleAntennaFeed]: Feed using specified transform");
        feedTransform = part.FindModelTransform(FeedTransformName);
        if (feedTransform == null)
        {
          Debug.LogError(String.Format("[NearFutureExploration] [ModuleAntennaFeed]: Could not find transform {0}", FeedTransformName));
        }
      } else
      {
        Debug.Log("[NearFutureExploration] [ModuleAntennaFeed]: Feed using offset and direction values");
      }

    }
    void SetupRenderer()
    {
      if (HighLogic.LoadedSceneIsEditor)
      {
        lineRenderable = false;
      } else
      {
        lineRenderable = false;
      }

      goodColor = new Color(0f, 1f, 1f, .5f);
      badColor = new Color(1f, 0f, 0f, .5f);

      renderedLine = new DebugLine(1f, badColor);
      if (feedTransform)
      {
        renderedLine.XForm.parent = feedTransform;
        renderedLine.XForm.localRotation = Quaternion.identity ;
        renderedLine.XForm.localPosition = Vector3.zero;
      }
      else
      {
        renderedLine.XForm.parent = part.partTransform;
        renderedLine.XForm.localRotation = Quaternion.LookRotation(feedVector);
        renderedLine.XForm.localPosition = feedOffset;
      }
      renderedLine.SetVisibility(lineRenderable);
    }
    void FindAntenna()
    {
      baseAntenna = part.GetComponent<ModuleDataTransmitterFeedeable>();
      deployModule = part.GetComponent<ModuleDeployableAntenna>();
      if (baseAntenna == null)
      {
        Debug.LogError("[NearFutureExploration] [ModuleAntennaFeed]: Could not find an antenna module for use as feeder");

      } else
      {
        if (deployModule != null)
        {
          Debug.Log("[NearFutureExploration] [ModuleAntennaFeed]: Feed is deployable");
          deployable = true;
        }
      }
      

      baseAntennaRange = baseAntenna.antennaPower;
    }
    public void ApplyReflectorBonus()
    {
      baseAntenna.antennaPower = baseAntennaRange + reflector.GetReflectorBonus() * FeedScale;
      baseAntenna.savedAntennaPower = baseAntennaRange + reflector.GetReflectorBonus() * FeedScale;
      StatusString = Localizer.Format("#LOC_NFEX_ModuleAntennaFeed_Field_ReflectorBuff_StatusString", (reflector.GetReflectorBonus() * FeedScale).ToString("F0"));
      TargetString = Localizer.Format("<<1>>", reflector.part.partInfo.title);
      //baseAntenna.powerText = String.Format(baseAntenna.antennaPower);
    }
    public void NullReflectorBonus()
    {
      baseAntenna.antennaPower = baseAntennaRange;

      StatusString = "";
      TargetString = "";
      //baseAntenna.powerText = String.Format(baseAntenna.antennaPower);
    }
    public bool TestLOSAll(out ModuleDeployableReflector targetReflector)
    {
      targetReflector = null;
      LayerMask mask = 1 << LayerMask.NameToLayer("Default");

      // Cast the ray!
      RaycastHit[] hits = Physics.RaycastAll(RaycastStartPoint + RaycastDirection.normalized * RayDistance, -RaycastDirection, RayDistance, mask, QueryTriggerInteraction.Collide);

      if (hits.Length > 0 )
      {
        //Debug.Log(String.Format("[NearFutureExploration] [ModuleAntennaFeed]: Num Hits {0}", hits.Length));

        float maxDist = 0f;
        RaycastHit minHit = hits[0];

        for (int i = 0; i < hits.Length; i++)
        {
          //Debug.Log(String.Format("[NearFutureExploration] [ModuleAntennaFeed]: Hit {0} with distance {1}", hits[i].collider, hits[i].distance));
          if (hits[i].rigidbody == part.Rigidbody)
          { }
          else
          {

            if (hits[i].distance > maxDist)
            {
              minHit = hits[i];
              maxDist = hits[i].distance;
            }
          }
        }

        targetReflector = minHit.collider.gameObject.GetComponentInParent<ModuleDeployableReflector>();
        //Debug.Log(String.Format("[NearFutureExploration] [ModuleAntennaFeed]: Chose {0}", minHit.collider));
        if (targetReflector != null)
        {
          for (int i = 0; i < targetReflector.dishColliders.Count; i++)
          {
            if (minHit.collider == targetReflector.dishColliders[i] && targetReflector.deployState == ModuleDeployablePart.DeployState.EXTENDED)
            {
              if (lineRenderable)
                renderedLine.SetVisibility(true);
              renderedLine.AdjustSize(RayDistance - minHit.distance);
              renderedLine.SetColor(goodColor);
              return true;
            }
          }

        }
        if (lineRenderable)
          renderedLine.SetVisibility(true);
        if (minHit.rigidbody == part.Rigidbody)
          renderedLine.AdjustSize(30f);
        else
          renderedLine.AdjustSize(RayDistance-minHit.distance);
        renderedLine.SetColor(badColor);
        return false;

      }
      else
      {
        if (lineRenderable)
          renderedLine.SetVisibility(true);
        renderedLine.AdjustSize(30f);
        renderedLine.SetColor(badColor);
      }
      return false;
    }
  }
}
