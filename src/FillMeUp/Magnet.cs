using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PrefabEvolution;
using SimpleJSON;
using UnityEngine;
using Utils = MacGruber.Utils;

namespace CheesyFX
{
    public class Magnet : MonoBehaviour
    {
	    private bool initialized;
	    private Orifice orifice;
	    public Penetrator penetrator;
	    private Rigidbody rb;
	    private Vector3 force;
	    private Vector3 torque;
	    public IEnumerator zeroForce;
	    public IEnumerator zeroTorque;
	    public bool updateForce = true;
	    private Func<Vector3> GetInDirection;
	    private float timer;
	    public JSONStorableBool enabledJ = new JSONStorableBool("Enabled", true);
	    public JSONStorableFloat maxForce = new JSONStorableFloat("Max Magnetic Force", 100000f, 0f, 200000f);
	    public JSONStorableFloat maxTorque = new JSONStorableFloat("Max Magnetic Torque", 50f, 0f, 2000f);
	    public JSONStorableFloat engageQuickness = new JSONStorableFloat("Engage Quickness", 1f, 0f, 10f);
	    public JSONStorableFloat retreatQuickness = new JSONStorableFloat("Retreat Quickness", 1f, 0f, 10f);

	    private float engageQuicknessForce;
	    private float engageQuicknessTorque;


	    public List<object> UIElements = new List<object>();

	    public Magnet Init(Orifice orifice)
	    {
		    this.orifice = orifice;
		    GetInDirection = () => transform.up;
		    if (this.orifice is Throat)
		    {
			    rb = FillMeUp.atom.rigidbodies.First(x => x.name == "head");
		    }
		    else
		    {
			    rb = FillMeUp.atom.rigidbodies.First(x => x.name == "hip");
		    }
			engageQuickness.AddCallback(val =>
			{
				engageQuicknessForce = .1f * val;
				engageQuicknessTorque = .8f * val;
			});
			enabledJ = orifice.magnetic;
		    enabled = false;
		    initialized = true;
		    return this;
	    }

	    private void OnEnable()
	    {
		    if(!initialized) return;
		    zeroForce.Stop();
		    zeroTorque.Stop();
		    if (Vector3.Dot(penetrator.forward(), transform.position - penetrator.tip.position) < 0f) enabled = false;
	    }

	    private void OnDisable()
	    {
		    if(!initialized) return;
		    zeroForce.Stop();
		    zeroTorque.Stop();
		    zeroForce = ZeroForce().Start();
		    zeroTorque = ZeroTorque().Start();
		    getOut = false;
		    // "OnDisable".Print();
	    }

	    private bool getOut;
	    private void FixedUpdate()
	    {
		    if (Pose.isApplying || SuperController.singleton.freezeAnimation)
		    {
			    force = torque = Vector3.zero;
			    return;
		    }
		    var delta = transform.position - penetrator.tip.position;
		    if(updateForce)
		    {
			    if (Vector3.Dot(penetrator.forward(), delta) < 0f)
			    {
				    getOut = true;
				    timer = 2f;
			    }

			    if (getOut)
			    {
				    timer -= Time.fixedDeltaTime;
				    force += transform.up;
				    torque = Vector3.Lerp(torque, Vector3.zero, Time.fixedDeltaTime * retreatQuickness.val);
				    if (timer < 0f)
				    {
					    getOut = false;
				    }
			    }
			    else
			    {
				    var pen = penetrator.forward();
				    force = Vector3.Lerp(force, Vector3.Cross(Vector3.Cross(delta, pen), pen) * maxForce.val, engageQuicknessForce*Time.fixedDeltaTime);
				    torque = Vector3.Lerp(torque, Vector3.Cross(GetInDirection(), pen) * maxTorque.val, engageQuicknessTorque*Time.fixedDeltaTime);
			    }
		    }
		    
		    rb.AddForce(force, ForceMode.Force);
		    rb.AddTorque(torque,  ForceMode.Force);
		    
	    }

	    private IEnumerator ZeroForce()
	    {
		    var wait = new WaitForFixedUpdate();
		    while (!Pose.isApplying && !SuperController.singleton.freezeAnimation && Mathf.Abs(force.sqrMagnitude) > .001f)
		    {
			    yield return wait;
			    force = Vector3.Lerp(force, Vector3.zero, Time.fixedDeltaTime * retreatQuickness.val);
			    rb.AddForce(force, ForceMode.Force);
		    }
		    force = Vector3.zero;
	    }
	    
	    private IEnumerator ZeroTorque()
	    {
		    var wait = new WaitForFixedUpdate();
		    while (!Pose.isApplying && !SuperController.singleton.freezeAnimation && Mathf.Abs(torque.sqrMagnitude) > .001f)
		    {
			    yield return wait;
			    torque = Vector3.Lerp(torque, Vector3.zero, Time.fixedDeltaTime * retreatQuickness.val);
			    rb.AddTorque(torque, ForceMode.Force);
		    }
		    torque = Vector3.zero;
	    }

	    public void CreateUINewPage()
	    {
		    FillMeUp.singleton.ClearUI();
		    UIElements.Clear();
		    var button = FillMeUp.singleton.CreateButton("Return");
		    button.buttonColor = new Color(0.55f, 0.90f, 1f);
		    button.button.onClick.AddListener(
			    () =>
			    {
				    FillMeUp.singleton.ClearUI();
				    FillMeUp.singleton.CreateUI();
				    FillMeUp.singleton.settingsTabbar.SelectTab(4);
			    });
		    UIElements.Add(button);
		    var info = Utils.SetupInfoTextNoScroll(FillMeUp.singleton, $"{orifice.name} Magnet", 50f, true);
		    info.text.alignment = TextAnchor.MiddleCenter;
		    // info.text.fontSize = 34;
		    info.background.offsetMin = new Vector2(0, 0);
		    enabledJ.CreateUI(UIElements, true);
		    maxForce.CreateUI(UIElements);
		    maxTorque.CreateUI(UIElements);
		    engageQuickness.CreateUI(UIElements);
		    retreatQuickness.CreateUI(UIElements);
	    }

	    public JSONClass Store()
	    {
		    var jc = new JSONClass();
		    maxForce.Store(jc);
		    maxTorque.Store(jc);
		    engageQuickness.Store(jc);
		    retreatQuickness.Store(jc);
		    return jc;
	    }

	    public void Load(JSONClass jc)
	    {
		    maxForce.Load(jc);
		    maxTorque.Load(jc);
		    engageQuickness.Load(jc);
		    retreatQuickness.Load(jc);
	    }
    }
}