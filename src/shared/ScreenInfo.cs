using System;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Battlehub.RTSaveLoad.PersistentObjects;
using MeshVR;
using MVR.FileManagementSecure;
using Request = MeshVR.AssetLoader.AssetBundleFromFileRequest;

namespace CheesyFX {
	public class ScreenInfo : MonoBehaviour {

		// IMPORTANT - DO NOT make custom enums. The dynamic C# complier crashes Unity when it encounters these for
		// some reason
		
		protected Canvas dynCanvas;
		private GameObject g;
		private GameObject capsule;
		public Text dynText;
		public float timer = 4f;
		private Color textColor = new Color(1f, 0f, 0.35f);
		private Color barColor = new Color();

		private Mesh wheelMesh;
		private Material wheelMaterial;
		private Material capMat;
		private Color capColor;

		private Color imageColor;
		private GameObject bar;
		private RectTransform barRT;

		private StimBar stimBar;
		private RBSprayer _rbSprayer;

		public ScreenInfo Init() {
			g = new GameObject("ScreenInfoCanvas");
			dynCanvas = g.AddComponent<Canvas>();
			dynCanvas.renderMode = RenderMode.WorldSpace;
			// only use AddCanvas if you want to interact with the UI - no needed if display only
			//SuperController.singleton.AddCanvas(dynCanvas);
			CanvasScaler cs = g.AddComponent<CanvasScaler>();
			// cs.scaleFactor = 1.0f;
			cs.dynamicPixelsPerUnit = 1f;
			// GraphicRaycaster gr = g.AddComponent<GraphicRaycaster>();
			RectTransform rt = g.GetComponent<RectTransform>();
			// rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 200f);
			// rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 50f);
			g.transform.localScale = new Vector3(0.0006f, 0.0006f, 0.0006f);
			// g.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
			g.transform.localPosition = new Vector3(.15f, .15f, .5f);
			
			// capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
			// capsule.gameObject.transform.SetParent(g.transform, false);
			// capsule.transform.localPosition -= new Vector3(10f, 0f, 0f);
			// capsule.transform.localScale = new Vector3(25f, 25f, 25f);
			// capsule.transform.localEulerAngles = new Vector3(0f, 0f, 90f);
			// var mesh = capsule.GetComponent<MeshFilter>().mesh;
			// var verts = mesh.vertices;
			// for (int i = 0; i < verts.Length; i++)
			// {
			// 	if (verts[i].y > 0f)
			// 	{
			// 		verts[i] += new Vector3(0f, 5f, 0f);
			// 	}
			// }
			// capMat = capsule.GetComponent<Renderer>().material;
			// capColor = capMat.color = new Color(1f, 0.29f, 0.39f);
			// mesh.vertices = verts;
			// // capMat.shader = Shader.Find("Standard (Specular setup)");
			// capMat.SetFloat("_Offset", 1f);
			// capMat.SetFloat("_MinAlpha", 0f);

			// stimBar = new StimBar(g);
			
			// var bar = new GameObject();
			// var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
			// bar.transform.SetParent(g.transform, true);
			// bar.GetComponent<Renderer>().material.shader = Shader.Find("Battlehub/RTGizmos/Handles");
			// bar.transform.localScale = new Vector3(1000f, 1000f, 1000f);
			// var barRT = bar.AddComponent<RectTransform>();
			// barRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 200f);
			// barRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 50f);
			// bar = new GameObject();
			// bar.transform.SetParent(g.transform, false);
			// var image = bar.AddComponent<Image>();
			// // SetupSprite(image, TouchMe.packageUid+"Custom/Scripts/CheesyFX/BodyLanguage/Sprites/heart.jpg" );
			// image.material.color = Color.white;
			// barRT = bar.GetComponent<RectTransform>();
			// barRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 200f);
			// barRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 50f);
			// barRT.anchorMax = Vector2.one;
			// barRT.anchorMin = Vector2.zero;
			// barRT.offsetMax = Vector2.zero;
			// barRT.offsetMin = Vector2.zero;
			//
			// slider = g.AddComponent<Slider>();
			// slider.interactable = false;
			// slider.transition = Selectable.Transition.None;
			// slider.fillRect = barRT;
			// slider.navigation = Navigation.defaultNavigation;
			
			// anchor to head for HUD effect
			Transform headCenter = SuperController.singleton.centerCameraTarget.transform;
			rt.SetParent(headCenter, false);
			
			GameObject g2 = new GameObject();
			g2.name = "Text";
			g2.transform.parent = g.transform;
			g2.transform.localScale = Vector3.one;
			g2.transform.localPosition = Vector3.zero;
			g2.transform.localRotation = Quaternion.identity;
			Text t = g2.AddComponent<Text>();
			RectTransform rt2 = g2.GetComponent<RectTransform>();
			rt2.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 100f);
			rt2.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 100f);
			t.alignment = TextAnchor.MiddleCenter;
			t.horizontalOverflow = HorizontalWrapMode.Overflow;
			t.verticalOverflow = VerticalWrapMode.Overflow;
			Font ArialFont = (Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");
			t.font = ArialFont;
			t.fontSize = 24;
			t.text = "Test";
			t.enabled = true;
			t.color = textColor;
			dynText = t;
			g.name = "Canvas";
			
			CreateWheelMesh();
			// sprayer = g.AddComponent<Sprayer>().Init();
			enabled = false;
			
			return this;
		}


		private void SetupSprite(Image image, string path, bool preserveAspect = true)
		{
			var tex = new Texture2D(2, 2);
			var data = FileManagerSecure.ReadAllBytes(path);
			tex.LoadImage(data);
			tex.Apply();
			image.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0f, 0f), 100.0f);
			image.preserveAspect = true;
		}

		private void CreateWheelMesh()
		{
			float a = Mathf.Sqrt(3) / 2;
			Vector3[] vertices =
			{
				new Vector3(.5f, 1f, 0f),
				new Vector3(-.5f, 1f, 0f),
				new Vector3(.5f, .5f, a),
				new Vector3(-.5f, .5f, a),
				new Vector3(.5f, -.5f, a),
				new Vector3(-.5f, -.5f, a),
				new Vector3(.5f, -1f, 0f),
				new Vector3(-.5f, -1f, 0f),
				new Vector3(.5f, -.5f, -a),
				new Vector3(-.5f, -.5f, -a),
				new Vector3(.5f, .5f, -a),
				new Vector3(-.5f, .5f, -a),
			};
			
			
			vertices = vertices.Select(x => x * 30f).ToArray();
			wheelMesh = new Mesh();
			// var idx = Enumerable.Range(0, vertices.Length).ToArray();
			int [] triangles = new[]
			{
				0,1,2,
				1,3,2,
				3,4,2,
				3,5,4,
				5,6,4,
				5,7,6,
				7,8,6,
				7,9,8,
				9,10,8,
				9,11,10,
				11,0,10,
				11,1,0,
				
				1,5,3,
				1,7,5,
				1,9,7,
				1,11,9,
				
				0,8,10,
				0,6,8,
				0,4,6,
				0,2,4,
			};
			wheelMesh.vertices = triangles.Select(x => vertices[x]).ToArray();
			int[] idx = Enumerable.Range(0, wheelMesh.vertices.Length).ToArray();
			wheelMesh.normals = new Vector3[]
			{
				new Vector3(0f, a, .5f),
				new Vector3(0f, a, .5f),
				new Vector3(0f, a, .5f),
				new Vector3(0f, a, .5f),
				new Vector3(0f, a, .5f),
				new Vector3(0f, a, .5f),
				
				Vector3.forward, 
				Vector3.forward, 
				Vector3.forward, 
				Vector3.forward, 
				Vector3.forward, 
				Vector3.forward, 
				
				new Vector3(0f, -a, .5f),
				new Vector3(0f, -a, .5f),
				new Vector3(0f, -a, .5f),
				new Vector3(0f, -a, .5f),
				new Vector3(0f, -a, .5f),
				new Vector3(0f, -a, .5f),
				
				new Vector3(0f, -a, -.5f),
				new Vector3(0f, -a, -.5f),
				new Vector3(0f, -a, -.5f),
				new Vector3(0f, -a, -.5f),
				new Vector3(0f, -a, -.5f),
				new Vector3(0f, -a, -.5f),
				
				Vector3.back, 
				Vector3.back, 
				Vector3.back, 
				Vector3.back, 
				Vector3.back, 
				Vector3.back, 
				
				new Vector3(0f, a, -.5f),
				new Vector3(0f, a, -.5f),
				new Vector3(0f, a, -.5f),
				new Vector3(0f, a, -.5f),
				new Vector3(0f, a, -.5f),
				new Vector3(0f, a, -.5f),
				
				Vector3.left, 
				Vector3.left,
				Vector3.left,
				Vector3.left,
				Vector3.left,
				Vector3.left,
				Vector3.left,
				Vector3.left,
				Vector3.left,
				Vector3.left,
				Vector3.left,
				Vector3.left,
				
				Vector3.right, 
				Vector3.right, 
				Vector3.right, 
				Vector3.right, 
				Vector3.right, 
				Vector3.right, 
				Vector3.right, 
				Vector3.right, 
				Vector3.right, 
				Vector3.right, 
				Vector3.right, 
				Vector3.right, 
				
			};
			wheelMesh.normals = new []{
				Vector3.up,
				Vector3.up, 
				
				new Vector3(0f, .5f, a),
				new Vector3(0f, .5f, a),
			
				new Vector3(0f, -.5f, a),
				new Vector3(0f, -.5f, a),
				
				Vector3.down, 
				Vector3.down, 
				
				new Vector3(0f, -.5f, -a),
				new Vector3(0f, -.5f, -a),
			
				new Vector3(0f, .5f, -a),
				new Vector3(0f, .5f, -a),
				
			};
			// _mesh.uv = vertices.Select(x => new Vector2(x.x, x.y)).ToArray();
			wheelMesh.SetTriangles(idx, 0);
			// _mesh.SetIndices(idx.ToArray(), MeshTopology.Triangles, 0);
			// _mesh.RecalculateBounds();
			wheelMaterial = new Material(Shader.Find("Standard")) { color = Color.white };
			wheelMaterial.SetFloat("_Offset", 1f);
			wheelMaterial.SetFloat("_MinAlpha", 1f);
		}
		
		public void DrawScrollWheel()
		{
			//g.transform.rotation*Quaternion.Euler(angle, 0f, 0f)
			var matrix = Matrix4x4.TRS(g.transform.position, g.transform.rotation*Quaternion.Euler(-angle, 0f, 0f), g.transform.localScale);
			// matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, g.transform.localScale);
			Graphics.DrawMesh(mesh:wheelMesh, matrix:matrix, material:wheelMaterial, layer:0);
		}
		
		public void Run()
		{
			if(g != null)
			{
				g.SetActive(true);
				textColor.a = 1f;
				timer = 4f;
				enabled = true;
			}
		}

		private float angle;
		private bool rationStopped;
		private bool rotateUp = true;
		void Update()
		{
			if(!rationStopped)
			{
				if (rotateUp && angle < 62f) angle += 60f * Time.deltaTime;
				else
				{
					rotateUp = false;
					angle -= 25f * Time.deltaTime;
					if (angle <= 60f) rationStopped = true;
				}
			}
			// DrawScrollWheel();
			timer -= Time.deltaTime;
			if (timer < 0f)
			{
				enabled = false;
				rationStopped = false;
				rotateUp = true;
				angle = 0f;
				// ParticleSprayer.ps.Stop();
			}
			// particleSprayer.SetRate(timer);
			textColor.a = timer * .25f;
			dynText.color = textColor;
			// capColor.a = timer * .25f;
			// capMat.color = capColor;

			// bar.transform.localScale = new Vector3(timer, 1f, 1f);
			// stimBar.val = ReadMyLips.stimulation.val;

			
		}

		private void OnDisable()
		{
			g.SetActive(false);
		}

		public void OnDestroy() {
			if (dynCanvas != null) {
				//SuperController.singleton.RemoveCanvas(dynCanvas);
				Destroy(g);
			}
			Destroy(capsule);
		}

	}
}