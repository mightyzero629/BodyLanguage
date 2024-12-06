using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using MacGruber;
using SimpleJSON;
using Random = UnityEngine.Random;

namespace CheesyFX
{
    public class Orifice : Fuckable
	{
		private readonly int id;
		
		public JSONStorableBool preventSwitching = new JSONStorableBool("Prevent Switching", true);
		public JSONStorableBool enabledJ = new JSONStorableBool("Enabled", true);
		public JSONStorableBool autoTogglePenisTip = new JSONStorableBool("Auto toggle Penis Tip", false);
		public JSONStorableBool showProximity = new JSONStorableBool("Show Proximity", false);

		public JSONStorableFloat bulgeScale = new JSONStorableFloat("Bulge Scale", 10f, 0f, 20f, false);
		public JSONStorableFloat bulgeDepthScale = new JSONStorableFloat("Bulge Depth Scale", 1f, 0f, 4f, false);
		public JSONStorableFloat bulgeSharpness = new JSONStorableFloat("Bulge Sharpness", .5f, 0f, 2f);
		
		public JSONStorableFloat triggerScale = new JSONStorableFloat("Enter Trigger Scale", 1f, 0f, 3f);
		public JSONStorableFloat triggerOffsetUp = new JSONStorableFloat("Enter Trigger Offset Up", 0f, -.2f, .2f);
		public JSONStorableFloat triggerOffsetForward = new JSONStorableFloat("Enter Trigger Offset Forward", 0f, -.2f, .2f);
		public JSONStorableFloat proximityScale = new JSONStorableFloat("Proximity Scale", .1f, .01f, .2f);
		
		public JSONStorableFloat stretch = new JSONStorableFloat("Stretch", 0f, 0f, 1f, true, false);
		public JSONStorableFloat relaxation;
		
		public JSONStorableFloat stretchScale = new JSONStorableFloat("Stretch Scale", 1f, 0f, 2f);
		
		public JSONStorableFloat depthStimulationThreshold = new JSONStorableFloat("Depth Stimulation Threshold (ReadMyLips)", .10f, 0f, .5f, true);

		public JSONStorableBool correctiveTorqueEnabled = new JSONStorableBool("Corrective Torque Enabled", true);
		public JSONStorableFloat correctiveYaw = new JSONStorableFloat("Corrective Yaw Scale", 200f, 0f, 500f);
		public JSONStorableFloat correctivePitch = new JSONStorableFloat("Corrective Pitch Scale", 50f, 0f, 500f);
		
		
		// public JSONStorableBool stiffenPenis = new JSONStorableBool("Stiffen Penis", true);
		// public JSONStorableFloat stiffenAmount = new JSONStorableFloat("StiffenAmount", 200f, 50f, 500f);
		
		protected FloatTriggerManager stretchTriggers;
		protected FloatTriggerManager relaxationTriggers;
		
		private UIDynamicButton stretchTriggerButton;
		private UIDynamicButton relaxationTriggerButton;
		
		public EventTrigger onProximityEnter = new EventTrigger(FillMeUp.singleton, "On Proximity Enter");
		public EventTrigger onProximityExit = new EventTrigger(FillMeUp.singleton, "On Proximity Exit");
		
		public ProximityHandler proximityHandler;
		
		public Orifice other;

		private bool _isPenetrated;
		
		public Rigidbody depthReference;

		public GameObject depthMeter;
		public Renderer depthMeterRenderer;
		public GameObject entrance;
		public GameObject enterTriggerGO;
		public Vector3 enterTriggerBasePosition;
		public Collider enterTriggerCollider;
		public OrificeTriggerHandler orificeTriggerHandler;
		public float[] bulgeTargets = new float[8];
		
		private Vector3 correctiveTorque;
		private IEnumerator zeroCorrectiveTorque;

		public ParticleSystem ps;
		public ParticleSystem ps1;

		public Transform guidance;

		protected WaitForFixedUpdate waitForFixedUpdate = new WaitForFixedUpdate();

		public Collider lastColliding
		{
			get { return orificeTriggerHandler.lastColliding; }
			set { orificeTriggerHandler.lastColliding = value; }
		}


		public override bool isPenetrated{
			get{return _isPenetrated;}
			set
			{
				try
				{
					if(_isPenetrated == value) return;
					SetPenetrated(value);
				}
				catch (Exception e)
				{
					SuperController.LogError(e.ToString());
				}
			}
		}

		public virtual void SetPenetrated(bool val)
		{
			if (_isPenetrated == val) return;
			depthMeterRenderer.enabled = val && FillMeUp.debugMode;
			zeroSpeed.Stop();
			zeroStretch.Stop();
		
			if (!val)
			{
				IgnoreColliders(false);
				info.val = "Idle";
				if(autoTogglePenisTip.val) SetPenisTipState(false);
				thrustForce.ShutDown(4f);
				maleForce.ShutDown(2f);
				if (!(this is Throat))
				{
					IgnoreColliders(true);
				}
				StimReceiver receiver;
				if (FillMeUp.stimReceivers.TryGetValue(penetrator, out receiver))
				{
					receiver.SetFucking(null);
				}
				if(ReadMyLips.isOrgasmPleasure && penetrator.stimReceiver && ReadMyLips.singleton) penetrator.stimReceiver.ForceOrgasm(true); 
				if (penetrator?.type == 1)
				{
					var person = penetrator.stimReceiver as Person;
					person.xrayClient?.ShutDown();
				}
				// penetrator.SetTipAndWith();
				penetrator = null;
				penetratingAtom = null;
				depthMeter.transform.parent = null;
				foreski = null;
				depth.val = 0f;
				
				// GetStretch();
				// if(this is Vagina)
				// {
				// 	// zeroStretch = ZeroStretch().Start();
				// 	enabled = false;
				// }
				zeroSpeed = ZeroSpeed().Start();
				zeroCorrectiveTorque = ZeroCorrectiveTorque().Start();
				penetratorWidth = 0f;
				FillMeUp.UpdateBulge();
				magnet.updateForce = true;
				// depthForce.ShutDown();
				maleForce.ShutDown();
			}
			else
			{
				// penetrator.tip.Draw();
				zeroCorrectiveTorque.Stop();
				enabled = true;
				magnet.updateForce = false;
				if (other != null) other.magnet.enabled = false;
			
				if (thrustForce.enabledJ.val)
				{
					thrustForce.Enable();
					// depthForce.Enable();
				}
				if (penetrator.type > 0)
				{
					maleForce.rb = penetrator.root;
					if(maleForce.enabledJ.val) maleForce.Enable();
					if (penetrator.type == 1)
					{
						foreski = penetrator.stimReceiver.foreski;
						foreskiTimer = .5f;
					}
					else foreski = null;
				}

				if (!(this is Throat))
				{
					IgnoreColliders(true);
				}
				StimReceiver receiver;
				if (FillMeUp.stimReceivers.TryGetValue(penetrator, out receiver))
				{
					receiver.SetFucking(this);
				}

				if (penetrator != null && penetrator.type > 0)
				{
					if(!(this is Throat)) penetrator.stimReceiver.StiffenHalf();
					if (penetrator.type == 1)
					{
						var person = penetrator.stimReceiver as Person;
						person.xrayClient?.Enable(this);
					}
				}
				info.val = $"Penetrated by <b>{penetrator.atom.name}/{penetrator.tipCollider.attachedRigidbody.name}</b>";
			}
			_isPenetrated = val;
			FillMeUp.SetPenetrated();
		}

		public Atom penetratingAtom
		{
			set
			{
				FillMeUp.penetratingAtoms[this] = value;
			}
			get { return FillMeUp.penetratingAtoms[this]; }
		}
		

		public float penetratorWidth;

		private IEnumerator zeroSpeed;

		protected IEnumerator zeroStretch;
		// private bool _paused;
		// public bool paused
		// {
		// 	set
		// 	{
		// 		_paused = orificeTriggerHandler.enabled = value;
		// 	}
		// }

		public override void Init(string name)
		{
			base.Init(name);
			// SimpleTriggerHandler.LoadAssets();
			// depthTriggers = FillMeUp.singleton.AddFloatTriggerManager(depth, false, 0f, 0f, .25f);
			// speedTriggers = FillMeUp.singleton.AddFloatTriggerManager(speed, true, 0f, 0f, .5f);
			stretchTriggers = FillMeUp.singleton.AddFloatTriggerManager(stretch, false, 0f, 0f, .5f);
			if(relaxation != null) relaxationTriggers = FillMeUp.singleton.AddFloatTriggerManager(relaxation);

			CreateDepthMeter();
			
			enabledJ.setCallbackFunction += val =>
			{
				enabled = val;
				orificeTriggerHandler.enabled = val;
			};
			triggerOffsetUp.setCallbackFunction = x => UpdateTriggerPosition();
			triggerOffsetForward.setCallbackFunction = x => UpdateTriggerPosition();
			triggerScale.setCallbackFunction = UpdateTriggerScale;
			proximityScale.setCallbackFunction = UpdateProximityScale;
			showProximity.setCallbackFunction += val =>
			{
				proximityTrigger.gameObject.GetComponent<Renderer>().enabled = val;
			};

			depth.setCallbackFunction += val =>
			{
				for (int i = 0; i < bulgeTargets.Length; i++)
				{
					bulgeTargets[i] = Bulge(i);
				}
			};
			magnetic.setCallbackFunction += val =>
			{
				if (!val) magnet.enabled = false;
			};
			
			
			thrustForce.GetDirection = () => -maleThrustDirection;
			thrustForce.amplitude.mean.SetWithDefault(200f);
			thrustForce.amplitude.delta.SetWithDefault(80f);
			thrustForce.period.mean.SetWithDefault(.6f);
			// thrustForce.period.sharpness.SetWithDefault(3f);
			if (this is Throat)
			{
				thrustForce.paramControl.offset.SetWithDefault(100f);
				thrustForce.paramControl.offsetQuickness.SetWithDefault(1f);
			}
			else
			{
				thrustForce.paramControl.offset.SetWithDefault(150f);
				thrustForce.paramControl.offsetQuickness.SetWithDefault(2f);
			}

			// thrustForce.control.presetSystem.Init(false);
			// maleForce.control.presetSystem.Init(false);
			InitForcePresets();

			CreateTriggersUI();
			ClearUI();
			
			enabled = false;
			initialized = true;
		}

		public void CreateParticleSystem()
		{
			var go = new GameObject($"{name} ParticleSystem1");
			// go.transform.SetParent(entrance.transform, false);
			go.transform.parent = entrance.transform;
			go.transform.localPosition = Vector3.zero;
			go.transform.localRotation = Quaternion.identity;
			ps = go.AddComponent<ParticleSystem>();
			var shape = ps.shape;
			shape.rotation = new Vector3(90f, 0f, 0f);
			shape.position = new Vector3(0f, -.01f, 0f);
			
			StimReceiver.InitPS(ps);
			var main = ps.main;
			main.startSize = .002f;
			main.startSpeed = 0f;
			main.playOnAwake = false;
			var emission = ps.emission;
			emission.rateOverTime = 5f;

			go = new GameObject($"{name} ParticleSystem2");
			// go.transform.SetParent(ps.transform);
			go.transform.parent = entrance.transform;
			go.transform.localPosition = Vector3.zero;
			go.transform.localRotation = Quaternion.identity;
			ps1 = go.AddComponent<ParticleSystem>();
			StimReceiver.InitPS(ps1);
			main = ps1.main;
			main.startSize = .0005f;

			main = ps1.main;
			main.startSize = .001f;
			main.startSpeed = 0f;

			var subemitters = ps.subEmitters;
			subemitters.AddSubEmitter(ps1, ParticleSystemSubEmitterType.Birth, ParticleSystemSubEmitterProperties.InheritNothing);
			subemitters.enabled = true;
			var iv = ps1.inheritVelocity;
			iv.mode = ParticleSystemInheritVelocityMode.Initial;
			iv.curveMultiplier = .75f;
			iv.enabled = true;
		}

		public virtual float Bulge(int index)
		{
			if (depth.val == 0f) return 0f;
			float scaledDepth = depth.val * bulgeDepthScale.val * 8f;
			if (scaledDepth < .4f) return 0f;
			float exponent = 2f*bulgeSharpness.val * (index - scaledDepth-.4f);
			float divisor = 1/(1 + scaledDepth);
			float val = .25f*bulgeScale.val * penetratorWidth * (scaledDepth-.4f) * divisor * divisor * Mathf.Pow(2f, -exponent * exponent);
			if (index > scaledDepth) val /= (index - scaledDepth + 1f);
			return val;
		}

		private void IgnoreColliders(bool ignore)
		{
			if(penetrator == null) return;
			foreach (var collider in penetrator.colliders)
			{
				Physics.IgnoreCollision(FillMeUp.abdomen, collider, ignore);
				
				Physics.IgnoreCollision(FillMeUp.lGlute1_2, collider, ignore);
				Physics.IgnoreCollision(FillMeUp.lGlute1_7, collider, ignore);
				
				Physics.IgnoreCollision(FillMeUp.rGlute1_2, collider, ignore);
				Physics.IgnoreCollision(FillMeUp.rGlute1_7, collider, ignore);

				for (int i = 0; i < FillMeUp.physGlute.Length; i++)
				{
					Physics.IgnoreCollision(FillMeUp.physGlute[i], collider, ignore);
				}
			}
		}

		public virtual void OnDestroy()
		{
			if(!initialized) return;
			Destroy(entrance);
			Destroy(orificeTriggerHandler);
			Destroy(depthMeter);
			Destroy(audioSource);
			Destroy(proximityTrigger.gameObject);
			Destroy(magnet);
			onProximityEnter.Remove();
			onProximityExit.Remove();
			IgnoreColliders(false);
			zeroSpeed.Stop();
			zeroStretch.Stop();
			zeroCorrectiveTorque.Stop();
		}

		public void OnAtomRename()
		{
			// depthTriggers.ForEach(x => x.OnAtomRename());
			// stretchTriggers.ForEach(x => x.OnAtomRename());
			// speedTriggers.ForEach(x => x.OnAtomRename());
			// if (relaxationTriggers != null) relaxationTriggers.ForEach(x => x.OnAtomRename());
			onProximityEnter.SyncAtomNames();
			onProximityExit.SyncAtomNames();
		}
		
		public override void ResetPenetration()
		{
			maleForce.ShutDownImmediate();
			thrustForce.ShutDownImmediate();
			orificeTriggerHandler.Reset();
			ps.Stop();
			ps1.Stop();
			ResetBulge();
			_isPenetrated = false;
		}

		public virtual void ResetBulge()
		{
			for (int i = 0; i < FillMeUp.bellyBulgeMorphs.Count; i++)
			{
				FillMeUp.bellyBulgeMorphs[i].morphValue = 0f;
			}
		}

		public override JSONClass Store(string subScenePrefix, bool storeTriggers = true)
		{
			JSONClass jc = base.Store(subScenePrefix, storeTriggers);
			bulgeScale.Store(jc);
			bulgeSharpness.Store(jc);
			bulgeDepthScale.Store(jc);
			depthStimulationThreshold.Store(jc);
			autoTogglePenisTip.Store(jc);
			proximityScale.Store(jc);
			triggerScale.Store(jc);
			triggerOffsetForward.Store(jc);
			triggerOffsetUp.Store(jc);
			preventSwitching.Store(jc);
			stretchScale.Store(jc);
			correctiveTorqueEnabled.Store(jc);
			correctivePitch.Store(jc);
			correctiveYaw.Store(jc);
			
			jc["Magnet"] = magnet.Store();
			if(storeTriggers)
			{
				if (stretchTriggers != null) jc["StretchTriggers"] = stretchTriggers.Store();
				if (relaxationTriggers != null) jc["RelaxationTriggers"] = relaxationTriggers.Store();
				jc[onProximityEnter.Name] = onProximityEnter.GetJSON();
				jc[onProximityExit.Name] = onProximityExit.GetJSON();
			}
			return jc;
		}

		public override void Load(JSONClass jc, string subScenePrefix)
		{
			base.Load(jc, subScenePrefix);
			if(jc.HasKey(name))
			{
				JSONClass tc = jc[name].AsObject;
				bulgeScale.Load(tc);
				bulgeSharpness.Load(tc);
				bulgeDepthScale.Load(tc);
				depthStimulationThreshold.Load(tc);
				autoTogglePenisTip.Load(tc);
				proximityScale.Load(tc);
				triggerScale.Load(tc);
				triggerOffsetForward.Load(tc);
				triggerOffsetUp.Load(tc);
				preventSwitching.Load(tc);
				stretchScale.Store(tc);
				correctiveTorqueEnabled.Load(tc);
				correctivePitch.Load(tc);
				correctiveYaw.Load(tc);
				
				if(tc.HasKey("Magnet")) magnet.Load(tc["Magnet"].AsObject);
				if (stretchTriggers != null) stretchTriggers.Load(tc["StretchTriggers"].AsObject);
				if (relaxationTriggers != null) relaxationTriggers.Load(tc["RelaxationTriggers"].AsObject);
				onProximityEnter.RestoreFromJSON(tc, subScenePrefix, false, true);
				onProximityExit.RestoreFromJSON(tc, subScenePrefix, false, true);
			}
			SyncTriggerButtons();
		}
		
		public override JSONClass StorePoseSettings(JSONClass parent)
		{
			JSONClass jc = base.StorePoseSettings(parent);
			triggerScale.Store(jc, false);
			triggerOffsetForward.Store(jc, false);
			triggerOffsetUp.Store(jc, false);
			proximityScale.Store(jc, false);
			correctiveTorqueEnabled.Store(jc, false);
			return jc;
		}
		
		public override void LoadPoseSettings(JSONClass baseJsonClass)
		{
			base.LoadPoseSettings(baseJsonClass);
			if (!baseJsonClass.HasKey(name))
			{
				triggerScale.SetValToDefault();
				triggerOffsetUp.SetValToDefault();
				triggerOffsetForward.SetValToDefault();
				proximityScale.SetValToDefault();
				correctiveTorqueEnabled.SetValToDefault();
				return;
			}
			JSONClass jc = baseJsonClass[name].AsObject;
			// baseJsonClass[name].Print();
			triggerScale.Load(jc, true);
			triggerOffsetForward.Load(jc, true);
			triggerOffsetUp.Load(jc, true);
			proximityScale.Load(jc, true);
			correctiveTorqueEnabled.Load(jc, true);
		}

		public void CreateDepthMeter()
		{
			if(depthMeter != null) Destroy(depthMeter);
			depthMeter = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			depthMeter.transform.localScale = new Vector3(.005f, .005f, .005f);
			Destroy(depthMeter.GetComponent<Collider>());
			depthMeterRenderer = depthMeter.GetComponent<Renderer>();
			depthMeterRenderer.material.shader = FillMeUp.debugShader;
			switch (name)
			{
				case "Anus":
					depthMeterRenderer.material.color = new Color(1f, 0f, 0.85f, .1f);
					break;
				case "Vagina":
					depthMeterRenderer.material.color = new Color(0f, 0.89f, 1f);
					break;
				default:
					depthMeterRenderer.material.color = new Color(1f, 0.5f, 0f, .2f);
					break;
			}
			depthMeterRenderer.enabled = false;
		}

		void UpdateTriggerPosition()
		{
			entrance.transform.localPosition = enterTriggerBasePosition + new Vector3(0f, triggerOffsetUp.val, triggerOffsetForward.val);
		}

		private void UpdateTriggerScale(float val)
		{
			enterTriggerGO.transform.localScale = .015f*val * Vector3.one;
		}

		private void UpdateProximityScale(float val)
		{
			if (!(this is Throat))
			{
				proximityTrigger.transform.localPosition = new Vector3(0f, -.45f*val, 0f);
				proximityTrigger.transform.localScale = .48f * val * Vector3.one;
			}
			else
			{
				proximityTrigger.transform.localPosition = new Vector3(0f, -.225f*val, -.0525f*val);
				proximityTrigger.transform.localScale = val * new Vector3(.8f,1f,.8f);
			}
		}
		
		public void SetupProximityTrigger()
		{
			GameObject proximityTriggerGO = GameObject.CreatePrimitive(PrimitiveType.Capsule);
			proximityTriggerGO.name = $"{name}_ProximityTrigger";
			Vector3 scale;
			proximityTriggerGO.transform.SetParent(entrance.transform, false);
			// proximityTriggerGO.transform.parent = entrance.transform;
			if (!(this is Throat))
			{
				proximityTriggerGO.transform.localPosition = new Vector3(0f, -.045f, 0f);
				scale = proximityScale.val * .48f * Vector3.one;
			}
			else
			{
				proximityTriggerGO.transform.localPosition = new Vector3(0f, -.0225f, -.00525f);
				scale = proximityScale.val * new Vector3(.8f,1f,.8f);
			}
			
			proximityTriggerGO.transform.localScale = scale;
			proximityTrigger = proximityTriggerGO.GetComponent<CapsuleCollider>();
			proximityTrigger.isTrigger = true;
			proximityTrigger.enabled = false;
			foreach (var collider in FillMeUp.atom.GetComponentsInChildren<Collider>(true))
			{
				Physics.IgnoreCollision(proximityTrigger, collider);
			}
			foreach (var carpal in FillMeUp.atom.rigidbodies.Where(x => x.name.Contains("Carpal")))
			{
			    foreach (var col in carpal.GetComponentsInChildren<Collider>())
			    {
			        if(col.attachedRigidbody == carpal) continue;
			        Physics.IgnoreCollision(proximityTrigger, col, false);
			    }
			}
			foreach (var thumb in FillMeUp.atom.rigidbodies.Where(x => x.name.Contains("Thumb")))
			{
				foreach (var col in thumb.GetComponentsInChildren<Collider>())
				{
					Physics.IgnoreCollision(proximityTrigger, col, false);
				}
			}

			if (this is Throat)
			{
				foreach (var col in FillMeUp.atom.rigidbodies.First(x => x.name == "Gen1").GetComponentsInChildren<Collider>(true))
				{
					Physics.IgnoreCollision(proximityTrigger, col, false);
				}
			}
			proximityTriggerGO.GetComponent<Renderer>().enabled = false;
			proximityHandler = proximityTrigger.gameObject.AddComponent<ProximityHandler>();
			proximityHandler.orifice = this;
			magnet = enterTriggerGO.AddComponent<Magnet>().Init(this);
			proximityTrigger.enabled = true;
		}

		public virtual void GetStretch()
		{
			if (isPenetrated)
			{
				if (Mathf.Abs(stretch.val - penetratorWidth) > .01f)
					stretch.val = Mathf.Lerp(stretch.val, penetratorWidth, 10f * Time.fixedDeltaTime);
				else stretch.val = penetratorWidth;
				
				// stretch.val = penetratorWidth;
			}
		}

		public void GetDepthAndSpeedSimple()
		{
			depth.val = Vector3.Dot(depthMeter.transform.position - enterPointTF.position, enterPointTF.up);
			var newSpeed = (depth.val - lastDepth) / Time.fixedDeltaTime;

			speed.val = Mathf.Lerp(speed.val, newSpeed, 10f * Time.fixedDeltaTime);
		}
		
		public void GetDepthAndSpeed()
		{
			if (penetrator.type == 0)
			{
				GetDepthAndSpeedSimple();
				return;
			}
			float dot = Vector3.Dot(penetrator.rigidbodies[2].transform.position - enterPointTF.position, enterPointTF.up);
			var sum = Vector3.Distance(penetrator.tip.position, enterPointTF.position);
			if (dot < 0f) sum += dot;
			for (int i = penetrator.rigidbodies.Count - 2; i > 0; i--)
			{
				var pos = penetrator.rigidbodies[i].transform.position;
				dot = Vector3.Dot(pos - enterPointTF.position, enterPointTF.up);
				if (dot > 0f)
				{
					sum += Vector3.Distance(pos, penetrator.rigidbodies[i+1].transform.position);
				}
				else
				{
					sum += Vector3.Distance(penetrator.rigidbodies[i+1].transform.position, enterPointTF.position);
					break;
				}
			}
			depth.val = sum;
			var newSpeed = (depth.val - lastDepth) / Time.fixedDeltaTime;

			speed.val = Mathf.Lerp(speed.val, newSpeed, 10f * Time.fixedDeltaTime);
		}
		
		private IEnumerator ZeroSpeed()
		{
			var wait = new WaitForFixedUpdate();
			while (Mathf.Abs(speed.val) > .01f)
			{
				speed.val = Mathf.Lerp(speed.val, 0f, 10f * Time.fixedDeltaTime);
				yield return wait;
			}
			speed.val = 0f;
		}

		protected IEnumerator ZeroStretch()
		{
			var wait = new WaitForFixedUpdate();
			while (Mathf.Abs(stretch.val) > .01f)
			{
				stretch.val = Mathf.Lerp(stretch.val, 0f, 10f * Time.fixedDeltaTime);
				yield return wait;
			}
			stretch.val = 0f;
		}
		
		private IEnumerator ZeroCorrectiveTorque()
		{
			while (correctiveTorque.sqrMagnitude > .00001f)
			{
				this.correctiveTorque = Vector3.Lerp(this.correctiveTorque, Vector3.zero, 2f*Time.fixedDeltaTime);
				var correctiveTorque = this.correctiveTorque.ScaleComponentsAlongUnit(rb.transform.up, rb.transform.right, correctiveYaw.val, correctivePitch.val);
				rb.AddTorque(correctiveTorque);
				yield return waitForFixedUpdate;
			}
		}

		public virtual float Stimulate(float baseStim = 0f, bool changeExpression = false)
		{
			float stimulus = baseStim + 320f * sensitivity.val * (1f + stretch.val) * Mathf.Abs(speed.val) * depth.val * Time.fixedDeltaTime;
			ReadMyLips.Stimulate(stimulus, changeExpression);
			return stimulus;
		}

		private float burstTimer;
		public override void FixedUpdate()
		{
			base.FixedUpdate();
			GetDepthAndSpeed();
			GetStretch();
			burstTimer -= Time.fixedDeltaTime;
			Stimulate();
			var moaning = ReadMyLips.PlayMoan();
			if (burstTimer < 0f)
			{
				burstTimer = .5f;
				if (ReadMyLips.randomBurstChance > 0f && ReadMyLips.randomBurstStrenght.val > 0f && Random.Range(0f, 1f) < ReadMyLips.randomBurstChance)
				{
					Stimulate(ReadMyLips.randomBurstStrenght.val, true);
				}
				if(!moaning) ReadMyLips.PlayBurstMoan();
			}
			if(correctiveTorqueEnabled.val)
			{
				correctiveTorque = Vector3.Lerp(this.correctiveTorque, Vector3.Cross(entrance.transform.up, maleThrustDirection), .5f * Time.fixedDeltaTime);
				var torque = correctiveTorque.ScaleComponentsAlongUnit(entrance.transform.forward, entrance.transform.right, correctiveYaw.val, correctivePitch.val);
				// correctiveTorque.Print();
				rb.AddTorque(torque);
			}
			if (preventPullout.val && depth.val < .3f*Mathf.Abs(speed.val)*penetrator.length)
			{
				thrustForce.ForceInwards();
				maleForce.ForceInwards();
			}
		}

		public void DriveForeski()
		{
			if(foreski != null && Person.foreskiEnabled.val)
			{
				if (foreskiTimer > 0f) foreskiTimer -= Time.fixedDeltaTime;
				else
				{
					var delta = depth.val - lastDepth;
					if (delta > 0f) delta *= 1.05f;
					foreski.morphValue += 20f * (penetrator.length - .5f*depth.val) / penetrator.length * delta;
				}
			}
		}
		
		private CapsuleCollider capTip;

		public Penetrator GetPenetrator(Collider collider)
		{
			Penetrator newPenetrator;
			if (collider == null) return null;
			if (collider is MeshCollider)
			{
				if (!FillMeUp.penetratorByCollider.TryGetValue(collider, out newPenetrator))
				{
					// newPenetrator = FillMeUp.penetratorByCollider[collider] = FillMeUp.singleton.gameObject.AddComponent<MeshPenetrator>().Init((MeshCollider)collider);
					newPenetrator = FillMeUp.penetratorByCollider[collider] = new MeshPenetrator((MeshCollider)collider);
					newPenetrator.tip.position = enterPointTF.position;
				}

				newPenetrator.SetTipAndWith();
				return newPenetrator;
			}
			if (FillMeUp.penetratorByCollider.TryGetValue(collider, out newPenetrator))
			{
				if (newPenetrator is CapsulePenetrator)
				{
					((CapsulePenetrator)newPenetrator).SetTipAndWith();
				}
				newPenetrator.SetTipAndWith();
				return newPenetrator;
			}
			Rigidbody rb = collider.attachedRigidbody;
			// float dist = 0f;
			var tempTip = collider as CapsuleCollider;
			
			float dist = Vector3.Dot(tempTip.transform.position - enterPointTF.position, enterPointTF.up);
			foreach(var c in rb.transform.GetComponentsInChildren<CapsuleCollider>(true))
			{
				if(c.attachedRigidbody == null) continue;
				// float thisDist = Vector3.SqrMagnitude(c.transform.position - rb.transform.position);
				float thisDist = Vector3.Dot(c.transform.position - enterPointTF.position, enterPointTF.up);
				if (thisDist > dist)
				{
					dist = thisDist;
					tempTip = c;
				}
			}
			if (!FillMeUp.penetratorByTip.TryGetValue(tempTip, out newPenetrator))
			{
				// newPenetrator = FillMeUp.penetratorByTip[tempTip] = FillMeUp.singleton.gameObject.AddComponent<CapsulePenetrator>().Init(tempTip);
				newPenetrator = FillMeUp.penetratorByTip[tempTip] = new CapsulePenetrator(tempTip);
			}
			FillMeUp.penetratorByCollider[collider] = newPenetrator;
			newPenetrator.SetTipAndWith();
			return newPenetrator;
		}

		public virtual void RegisterCollision(Collider collider)
		{
			// $"{name} {GetRadialDistance(collider, enterPointTF)}".Print();
			// collider.transform.position.Print();
			// enterPointTF.position.Print();
			// name.Print();
			if (collider == null) return;
			if (FillMeUp.snapping)
			{
				FillMeUp.RegisterSnapCollision();
				return;
			}
			
			Penetrator newPenetrator = GetPenetrator(collider);
			// penetrator.tipCollider.Print();
			// other.penetrator.tipCollider.Print();
			if(!(this is Throat) && preventSwitching.val && other.penetrator == newPenetrator)
			{
				// $"{name} {collider}".Print();
				// $"last: {other.name}, failed {name}".Print();
				// penetrator = null;
				return;
			}

			penetrator = newPenetrator;
			
			// tip.Print();
			// $"orifice: {name}, tip {tip.capsule}".Print();
			// $"collider: {collider.name}, tip {tempTip.name}".Print();
			depthMeter.transform.parent = penetrator.tip;
			depthMeter.transform.localPosition = Vector3.zero;
			penetratorWidth = penetrator.width * stretchScale.val;
			
			isPenetrated = true;
			penetratingAtom = penetrator.atom;
		}
		
		public float GetRadialDistance()
		{
			float val;
			if (lastColliding == null)
			{
				val = 1000f;
			}
			else val = Vector3
				.Cross(lastColliding.transform.up, enterPointTF.position - lastColliding.transform.position)
				.sqrMagnitude;
			// $"RadialDistance {name}: {val}".Print();
			return val;
		}

		private void SetPenisTipState(bool val)
		{
			var penisTipCtrl = penetrator.atom.freeControllers.FirstOrDefault(x => x.name == "penisTipControl");
			if(!penisTipCtrl) return;
			if (val)
			{
				penisTipCtrl.currentPositionState = FreeControllerV3.PositionState.On;
				penisTipCtrl.currentRotationState = FreeControllerV3.RotationState.On;
			}
			else
			{
				penisTipCtrl.currentPositionState = FreeControllerV3.PositionState.Off;
				penisTipCtrl.currentRotationState = FreeControllerV3.RotationState.Off;
			}

			proximityHandler.on = val;
		}

		public virtual void DoReset()
		{
			depth.val = stretch.val = penetratorWidth = 0f;
			magnet.zeroForce.Stop();
			magnet.zeroTorque.Stop();
			depthMeter.transform.parent = null;
		}

		public override void StopForcesImmediate()
		{
			base.StopForcesImmediate();
			correctiveTorque = Vector3.zero;
		}

		public void ClearUI()
		{
			FillMeUp.singleton.RemoveUIElements(UIElements);
		}

		public override void SyncTriggerButtons()
		{
			base.SyncTriggerButtons();
			stretchTriggerButton.label = $"Stretch Triggers ({stretchTriggers.triggers.Count})";
			if (relaxation != null) relaxationTriggerButton.label = $"Relaxation Triggers ({relaxationTriggers.triggers.Count})";
		}

		public override void CreateTriggersUI()
		{
			depthTriggerButton = FillMeUp.singleton.CreateButton($"Depth Triggers ({depthTriggers.triggers.Count})");
			depthTriggerButton.buttonColor = new Color(0.45f, 1f, 0.45f);
			depthTriggerButton.button.onClick.AddListener(() =>
			{
				{
					FillMeUp.singleton.ClearUI();
					depthTriggers.OpenPanel(FillMeUp.singleton, FillMeUp.singleton.CreateUI);
				}
			});
			UIElements.Add(depthTriggerButton);
			depth.CreateUI(FillMeUp.singleton, UIElements:UIElements);
			
			speedTriggerButton = FillMeUp.singleton.CreateButton($"Speed Triggers ({speedTriggers.triggers.Count})", true);
			speedTriggerButton.buttonColor = new Color(0.45f, 1f, 0.45f);
			speedTriggerButton.button.onClick.AddListener(() =>
			{
				{
					FillMeUp.singleton.ClearUI();
					speedTriggers.OpenPanel(FillMeUp.singleton, FillMeUp.singleton.CreateUI);
				}
			});
			UIElements.Add(speedTriggerButton);
			speed.CreateUI(FillMeUp.singleton, UIElements: UIElements, rightSide: true);
			
			if(stretch != null)
			{
				stretchTriggerButton =
					FillMeUp.singleton.CreateButton($"Stretch Triggers ({stretchTriggers.triggers.Count})");
				stretchTriggerButton.buttonColor = new Color(0.45f, 1f, 0.45f);
				stretchTriggerButton.button.onClick.AddListener(() =>
				{
					{
						FillMeUp.singleton.ClearUI();
						stretchTriggers.OpenPanel(FillMeUp.singleton, FillMeUp.singleton.CreateUI);
					}
				});
				UIElements.Add(stretchTriggerButton);
				stretch.CreateUI(FillMeUp.singleton, UIElements: UIElements);
			}

			if (relaxation != null)
			{
				relaxationTriggerButton = FillMeUp.singleton.CreateButton($"Relaxation Triggers ({relaxationTriggers.triggers.Count})", true);
				relaxationTriggerButton.buttonColor = new Color(0.45f, 1f, 0.45f);
				relaxationTriggerButton.button.onClick.AddListener(() =>
				{
					{
						FillMeUp.singleton.ClearUI();
						relaxationTriggers.OpenPanel(FillMeUp.singleton, FillMeUp.singleton.CreateUI);
					}
				});
				UIElements.Add(relaxationTriggerButton);
				relaxation.CreateUI(FillMeUp.singleton, UIElements:UIElements, rightSide:true);
			}
			
			var textField = FillMeUp.singleton.CreateTextField(info, true);
			UIElements.Add(textField);
			textField.height = 50f;
			
			var button = FillMeUp.singleton.CreateButton("On Proximity Enter");
			button.buttonColor = new Color(0.45f, 1f, 0.45f);
			button.button.onClick.AddListener(onProximityEnter.OpenPanel);
			UIElements.Add(button);
			
			button = FillMeUp.singleton.CreateButton("On Proximity Exit");
			button.buttonColor = new Color(0.45f, 1f, 0.45f);
			button.button.onClick.AddListener(onProximityExit.OpenPanel);
			UIElements.Add(button);

			autoTogglePenisTip.CreateUI(FillMeUp.singleton, UIElements:UIElements);
		}
		
		public override void CreateSettingsUI()
		{
			depthStimulationThreshold.CreateUI(FillMeUp.singleton, UIElements: UIElements);
			sensitivity.CreateUI(FillMeUp.singleton, UIElements: UIElements, rightSide:true);
			stretchScale.CreateUI(FillMeUp.singleton, UIElements: UIElements);
			penetrationSoundsVolume.CreateUI(FillMeUp.singleton, UIElements: UIElements, rightSide:true);
		}
		
		
		
		public override void CreateForcesUI()
		{
			base.CreateForcesUI();
			correctiveTorqueEnabled.CreateUI(UIElements, true);
			correctiveYaw.CreateUI(UIElements, true);
			correctivePitch.CreateUI(UIElements, true);
		}

		public override void CreateDebugUI()
		{
			depth.CreateUI(UIElements, rightSide:true);
			triggerOffsetUp.CreateUI(UIElements);
			triggerOffsetForward.CreateUI(UIElements);
			triggerScale.CreateUI(UIElements);
			proximityScale.CreateUI(UIElements);
			showProximity.CreateUI(UIElements);
			// enabledJ.CreateUI(UIElements: UIElements);
			preventSwitching.CreateUI(UIElements);
			FillMeUp.singleton.SetupButton("Reset Penetration", false, FillMeUp.OnPoseSnap, UIElements);
			FillMeUp.singleton.SetupButton("Info", false, () =>
			{
				foreach (var orifice in FillMeUp.orifices)
				{
					if(orifice.isPenetrated) $"{orifice.name}: Collisions: {orifice.orificeTriggerHandler.numCollisions} PenetratedBy: {orifice.penetrator.tipCollider.name}".Print();
					else $"{orifice.name}: Collisions: {orifice.orificeTriggerHandler.numCollisions} PenetratedBy: None".Print();
				}
			}, UIElements);
			var textField = FillMeUp.singleton.CreateTextField(info, true);
			UIElements.Add(textField);
			textField.height = 50f;
			base.CreateDebugUI();
			// FillMeUp.singleton.SetupButton("Print All Registered Penetrators", true, FillMeUp.PrintPenetrators, UIElements);
			// FillMeUp.singleton.SetupButton("Reset Registered Penetrators", false, FillMeUp.ResetPenetrators, UIElements);
			// FillMeUp.singleton.SetupButton("Reset Penis Colliders", true, () =>
			// {
			// 	foreach (var person in FillMeUp.persons)
			// 	{
			// 		person.penetrator.colliders.ForEach(x => x.gameObject.GetComponent<AutoCollider>().AutoColliderSizeSet());
			// 	}
			// }, UIElements);
			// FillMeUp.singleton.SetupButton("Sync XRay", true, XRay.SyncSkins, UIElements);
			
		}
	}

    
}