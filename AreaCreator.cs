using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Text.RegularExpressions;
using System;
using eChrono;

public class AreaCreator : EditorWindow {

	#region VARIABLES

	string myScene = "Aerides";
	string myXml = "scenes";
	int myPoints = 1;
	float myAreaHeight=1f;
	float myRadius=5f;
	int addAreaSize=1;
		
	Color colorStart = Color.red;
	Color colorPoint = Color.green;
	
	List<GameObject> pointsOrdered = new List<GameObject>();

	GameObject targetObject;
	Texture2D areasTexture, filterTexture, mapTexture;
//	TextAsset myXmlAsset;

	GameObject previewMap,previeFilter;
	Camera previewKamera;
	Material previewMaterial;

	Vector3[] myPositions;

	string sceneCurrentName;
	private static Texture2D tex;

	bool forImport, forCreation, forExport;
	int myCreatedAreas=0;

	public enum AreaMode{MANY_POINTS, CUBE, CIRCLE, GRAPHIC, COLLIDERS, GRAPHIC_3D}
	public AreaMode areaMode = AreaMode.GRAPHIC_3D;

	TextAsset myXmlAsset;
	XmlDocument movementXml, menuXML;

	static AreaCreator myWindow;
	string[] __typeStrings;
	int __currentType = 0;
	int prevType=-1;
	#endregion

	#region INIT SETTINGS

	// Add menu named "My Window" to the Window menu
	[MenuItem ("Stathis/Area and Path Creator")]
	static void Init () {
		// Get existing open window or if none, make a new one:
		myWindow = (AreaCreator)EditorWindow.GetWindow (typeof (AreaCreator));
//		window.position = new Rect(Screen.width - 200f , Screen.height, 200f,200f);
		myWindow.Show();

//		if(SceneView.onSceneGUIDelegate != myWindow.OnSceneGUI)
//		{
//			SceneView.onSceneGUIDelegate += myWindow.OnSceneGUI;
//		} 
	}

//	public void OnSceneGUI(SceneView view){
//		Handles.Label(Vector3.zero, "Instance Id = " + view.GetInstanceID());
//	}
//
//	void OnDisable(){
//		if(SceneView.onSceneGUIDelegate == myWindow.OnSceneGUI)
//		{
//			SceneView.onSceneGUIDelegate -= myWindow.OnSceneGUI;
//		} 
//	}

	bool isFirstTime;

	void OnEnable(){
		isFirstTime=true;
		SetTexture ();
		FindXmls();
	}

	void FindXmls(){
		// Get all available prefabs
		string xmlsInProjectFolder = Application.dataPath + "/Resources/XML";
		string[] xmls = System.IO.Directory.GetFiles(xmlsInProjectFolder, "*.xml");
		__typeStrings = new string[xmls.Length];
		for (int i = 0; i < xmls.Length; i++)
		{
			// Anchor point prefabs need to start with anp_anchorPoint to have a nice name displayed
			string filename = System.IO.Path.GetFileNameWithoutExtension(xmls[i]);
			__typeStrings[i] = filename;
		}
	}
	
	void SetTexture(){
		sceneCurrentName = GetSceneName ();
		tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
		tex.SetPixel(0, 0, Color.black);
		tex.Apply();
	}

	#endregion
	
	void OnGUI () 
	{
		#region WINDOW TITLE
		//background color
		if (tex == null) {
			SetTexture ();
		}
		GUI.DrawTexture (new Rect (0, 0, maxSize.x, maxSize.y), tex, ScaleMode.StretchToFill);
		GUILayout.Label (copyright, EditorStyles.toolbarButton);

		GUILayout.Label ("Όνομα σκηνής ( "+sceneCurrentName+" )", EditorStyles.toolbarTextField);


		GUILayout.Label ("Area Settings", EditorStyles.boldLabel);
		GUILayout.Label ("", EditorStyles.boldLabel);

		#endregion

		#region Buttons Menu
		if(myCreatedAreas>0)
		{
			if(GUILayout.Button("Clear All Areas In Scene"))
			{
				if(EditorUtility.DisplayDialog("DELETE ALL AREAS!!", "ARE YOU SURE;", "YES" , "NO")){
					ImportAreas.DestroyAreas();
					myCreatedAreas=0;
				}
			}
		}

		if(GUILayout.Button("Import & Create Areas 3d mode") && !forImport){ 
			forImport = !forImport;
			forCreation = false;
			forExport = false;
		}

		if(GUILayout.Button("Create Areas 2d mode") && !forCreation){
			forImport =false;
			forCreation = !forCreation;
			forExport = false;
		}


		#endregion

		#region Import Areas
//		EditorGUILayout.Space();
//		EditorGUIUtility.LookLikeControls();

		if(forImport)
		{
			_Import();
		}

		#endregion

		#region Create Area

		if(forCreation)
		{
			GUILayout.Label ("", EditorStyles.boldLabel);
			GUILayout.Label ("New Area Settings", EditorStyles.boldLabel);

			areaMode = (AreaMode) EditorGUILayout.EnumPopup("Select Tool Method:", areaMode);

			if(areaMode==AreaMode.MANY_POINTS)
			{
				_Many_Points();
			}else
			if(areaMode==AreaMode.CIRCLE)
			{
				_Circle();
			}else
			if(areaMode==AreaMode.CUBE)
			{
				_Cube();
			}else
			if(areaMode==AreaMode.GRAPHIC)
			{
				_Graphic();
				
			}else
			if(areaMode==AreaMode.COLLIDERS)
			{
				_Colliders();
			}else
			if(areaMode==AreaMode.GRAPHIC_3D)
			{
				_Graphic_3D();
			}
		}


		#endregion

		#region Export Areas

		if(myCreatedAreas>0)
		{
			GUILayout.Label ("", EditorStyles.boldLabel);
			GUILayout.Label ("Export New Areas", EditorStyles.boldLabel);
			
			if(GUILayout.Button("Export Areas to txt")){

				GameObject[] gs = FindObjectsOfType(typeof(GameObject)) as GameObject[];

				int x=0;

				if(gs.Length>0){
					foreach(GameObject g in gs){
						if(g.name == "CIRCLE_AREA" || g.name == "SQUARE_AREA"){
							x++;
							break;
						}
					}
				}

				if(x>0){
					if(EditorUtility.DisplayDialog("Not completed areas detected!!", "Continue export?", "YES" , "NO")){
						ExportManyAreas.ExportSceneAreas();
						myCreatedAreas=0;
					}
				}else{
					ExportManyAreas.ExportSceneAreas();
					myCreatedAreas=0;
				}
			}
		}


		#endregion
	

	}

	#region PREVIEW MAP

	Light light;
	DrawAreas draw;
	bool isPreviewMode;

	void Preview(bool addDrawAbility){

		isPreviewMode = true;

		if(!light){
			light = new GameObject().AddComponent<Light>();
			light.transform.name = "Light";
			light.transform.eulerAngles = new Vector3(90f,0f,0f);
			light.intensity = 0.33f;
			light.type = LightType.Directional;
		}

		XmlNode myTerrainSize = movementXml.SelectSingleNode ("/movement/" + myScenesNames[epilegmeniSkini] + "/settings/terrainSize");
		Vector2 terrainSize = new Vector2(float.Parse(myTerrainSize["x"].InnerText),float.Parse(myTerrainSize["y"].InnerText));

		XmlNode myPivotPos = movementXml.SelectSingleNode ("/movement/" + myScenesNames[epilegmeniSkini] + "/sceneArea");

		Vector2 pos = Vector2.zero;

		if(myPivotPos==null){
			#if UNITY_EDITOR
			Debug.LogWarning("myPivotPos for "+myScenesNames[epilegmeniSkini]+" is Null");
			#endif
			return;
		}else{

			if(!string.IsNullOrEmpty(myPivotPos["posX"].InnerText) && !string.IsNullOrEmpty(myPivotPos["posZ"].InnerText)){
				pos = new Vector2(float.Parse(myPivotPos["posX"].InnerText),float.Parse(myPivotPos["posZ"].InnerText));
			}

		}

		//get map from movement xml using poi name
		XmlNode myMapFilterFile = movementXml.SelectSingleNode("movement/"+myScenesNames[epilegmeniSkini]+"/menu/mapFilter");
		
		string mapFilterFile = string.Empty;
		
		if (myMapFilterFile != null)
		{
			if (myMapFilterFile["file"].InnerText != "") {
				mapFilterFile = myMapFilterFile ["file"].InnerText;
			} else {
				mapFilterFile = "none";
			}
		}else{
			#if UNITY_EDITOR
			Debug.LogWarning("Map for "+myScenesNames[epilegmeniSkini]+" is Null");
			#endif
		}

		//get map from movement xml using poi name
		XmlNode myMapFile = movementXml.SelectSingleNode("movement/"+myScenesNames[epilegmeniSkini]+"/menu/map");

		string mapFile = string.Empty;

		if (myMapFile != null)
		{
			if (myMapFile["file"].InnerText != "") {
				mapFile = myMapFile ["file"].InnerText;
			} else {
				mapFile = "none";
			}
		}else{
			#if UNITY_EDITOR
			Debug.LogWarning("Map for "+myScenesNames[epilegmeniSkini]+" is Null");
			#endif
		}




		Texture texMap = Stathis.Tools_Load.LoadTexture("images/maps",mapFile);

		if(!texMap){
			Debug.LogError("map texture "+mapFile+" is not found in Resources/images/maps");
			return;
		}

		Vector2 mapSize = new Vector2(tex.width, tex.height);

		if(!previewMaterial){
			previewMaterial = new Material (Shader.Find(" Diffuse"));
		}

		previewMaterial.mainTexture = texMap;


		if(!previewMap){
			previewMap = GameObject.CreatePrimitive(PrimitiveType.Quad);
			previewMap.transform.eulerAngles = new Vector3(90f,0f,0f);
			previewMap.name="map";
		}

		if(!previeFilter){
			previeFilter = new GameObject();
			previeFilter.name = "MapFilter";
			SpriteRenderer spr = previeFilter.AddComponent<SpriteRenderer>();
			spr.sprite = Stathis.Tools_Load.LoadSpriteFromResources("images/maps",mapFilterFile);
			float x = terrainSize.x/20f;///10000f; //(((terrainSize.x/2f)/10f)*1000f)/1000f;
			float y = terrainSize.y/20f;///10000f;// (((terrainSize.y/2f)/10f)*1000f)/1000f;

			Debug.Log(terrainSize.x);
			Debug.Log(x);

			previeFilter.transform.eulerAngles = new Vector3(90f,0f,0f);

			previeFilter.transform.parent = previewMap.transform;
//
			x = x/terrainSize.x;
//			float xB = xA * 1000f;
//			float xT = xB/1000f;
//
			y = y/terrainSize.y;
//			float yB = xA * 1000f;
//			float yT = xB/1000f;

			Vector3 scal = new Vector3(x, y, 1f);
//			scal.x /=1000f;
//			scal.y /=1000f;

			previeFilter.transform.localScale = scal;// new Vector3(((x/terrainSize.x)*1000f)/1000f, ((y/terrainSize.y)*1000f)/1000f, 1f);
			
			
			Color c = spr.color;
			c.a=0.33f;
			spr.color=c;
		}

		if(addDrawAbility){
			if(previewMap.GetComponent<AreaGroup>()==null){
				previewMap.AddComponent<AreaGroup>();
			}
			if(previewMap.GetComponent<DrawAreas>()==null){
				previewMap.AddComponent<DrawAreas>();
			}
		}

		previewMap.GetComponent<MeshRenderer>().sharedMaterial = previewMaterial;

		previewMap.transform.position = new Vector3(pos.x, -1f, pos.y);
		previewMap.transform.localScale = new Vector3(terrainSize.x, terrainSize.y,1f);

		if(!previewKamera){
			previewKamera = Camera.main;
			if(!previewKamera){
				previewKamera = new GameObject().AddComponent<Camera>();
			}
			previewKamera.isOrthoGraphic=true;
			previewKamera.transform.eulerAngles = new Vector3(90f,0f,0f);
			previewKamera.backgroundColor = Color.black;
		}

		if(terrainSize.x>=terrainSize.y){
			previewKamera.orthographicSize = terrainSize.x/2f;
		}else{
			previewKamera.orthographicSize = terrainSize.y/2f;
		}

		previewKamera.transform.position = new Vector3(pos.x, 10f, pos.y);

		if(light.GetComponent<DrawAreas>() == null){
			draw = light.gameObject.AddComponent<DrawAreas>();
			draw.previewAreas=true;
		}
		draw.activeAreas = activeAreas;
		draw.previewAreas=true;
	}
	
	#endregion

	#region OnGUI FUNCTIONS

	#region IMPORT FROM XML


	void _Import(){
		GUILayout.Label ("", EditorStyles.boldLabel);
		

		

		if(!myXmlAsset){
			if(movementXml!=null){
				movementXml=null;
			}

			// Create a selection grid where the user can select the current type
			__currentType = EditorGUILayout.Popup("Choose XML",__currentType, __typeStrings);
			
			if(prevType!=__currentType){
				// Load the current xml
				string path = "Assets/Resources/XML/" + __typeStrings[__currentType] + ".xml";
				myXmlAsset =  Resources.LoadAssetAtPath(path, typeof(TextAsset)) as TextAsset;
				prevType=__currentType;
			}

				
			GUILayout.Label ("Or assign the xml with movement parameters", EditorStyles.boldLabel);
			myXmlAsset = (TextAsset) EditorGUILayout.ObjectField ("Movement XML", myXmlAsset, typeof(TextAsset), false);
		}
		else{

			if(movementXml==null){
				myXml = myXmlAsset.name;
				
				movementXml = new XmlDocument(); 
				
				string excludedComments = Regex.Replace(myXmlAsset.text, "(<!--(.*?)-->)", string.Empty);
				movementXml.LoadXml(excludedComments);
				
				XmlNodeList myScenes = movementXml.FirstChild.ChildNodes;
				
				myScenesNames = new string[myScenes.Count];
				
				if(myScenes.Count>0){
					for(int i=0; i<myScenes.Count; i++){
						myScenesNames[i] = myScenes[i].Name;
					}
				}
				epilegmeniSkini=0;
				
			}else{
				if(myXmlAsset.name!=myXml){
					movementXml=null;
					epilegmeniSkini=0;
					Array.Clear(myScenesNames,0,myScenesNames.Length);
				}
			}
			
			#region check if xml is the correct
			if(movementXml!=null)
			{
				//read gps ref point
				XmlNode sceneArea = movementXml.SelectNodes ("/movement/" + myScenesNames[epilegmeniSkini] + "/sceneArea")[0];
				if(sceneArea==null){

					if(isFirstTime){
						if(EditorUtility.DisplayDialog("Select the name of XML that is contains all map info!", "Select movement.xml", "OK")){
							myXmlAsset=null;
							movementXml=null;
							epilegmeniSkini=0;
							Array.Clear(myScenesNames,0,myScenesNames.Length);
						}
					}

//						if(EditorUtility.DisplayDialog("Wrong XML.. map info are missing!", "Set New Xml", "OK")){
//							myXmlAsset=null;
//							movementXml=null;
//							epilegmeniSkini=0;
//							Array.Clear(myScenesNames,0,myScenesNames.Length);
//						}


//					isFirstTime=false;
//					return;
				}


			}
			#endregion
			
			
			GUILayout.Label ("", EditorStyles.boldLabel);
			epilegmeniSkini = EditorGUILayout.Popup("Select Scene", epilegmeniSkini, myScenesNames);

			GUILayout.Label ("", EditorStyles.boldLabel);

			#region BUTTONS



			if(GUILayout.Button("Open Scene to edit")){
				LoadScene();
			}

			GUILayout.Label ("", EditorStyles.boldLabel);

			if(loadedScene==GetSceneName())
			{

				if(GUILayout.Button("Import All Off Site Data From Xml")){
					
					if(string.IsNullOrEmpty(myScene) || !myXmlAsset){
						if(EditorUtility.DisplayDialog("Empty fields!!", "Please fill the correct values", "OK")){}
					}else
					if(!myXmlAsset){
						if(EditorUtility.DisplayDialog("XML file is empty", "Please select a file", "OK")){}
					}else{
						if(EditorUtility.DisplayDialog("MUST DELETE ALL Created AREAS IN SCENE!!", "ARE YOU SURE;", "YES" , "NO")){
							ImportAreas.CreateSceneAreas(myScenesNames[epilegmeniSkini], myXml, 2);
							ImportDeadAreas.CreateSceneAreas(myScenesNames[epilegmeniSkini], myXml, 2);
							ImportPaths.CreateScenePaths(myScenesNames[epilegmeniSkini], myXml, 2);
						}
					}
				}
				
				if(GUILayout.Button("Import All OnSite Data From Xml")){

					if(string.IsNullOrEmpty(myScene) || !myXmlAsset){
						if(EditorUtility.DisplayDialog("Empty fields!!", "Please fill the correct values", "OK")){}
					}else
					if(!myXmlAsset){
						if(EditorUtility.DisplayDialog("XML file is empty", "Please select a file", "OK")){}
					}else{
						if(EditorUtility.DisplayDialog("MUST DELETE ALL Created AREAS IN SCENE!!", "ARE YOU SURE;", "YES" , "NO")){
							ImportAreas.CreateSceneAreas(myScenesNames[epilegmeniSkini], myXml, 1);
							ImportDeadAreas.CreateSceneAreas(myScenesNames[epilegmeniSkini], myXml, 1);
							ImportPaths.CreateScenePaths(myScenesNames[epilegmeniSkini], myXml, 1);
						}
					}

				}
				
				if(GUILayout.Button("Import Scene Area From Xml (gps check in menu)")){

					if(string.IsNullOrEmpty(myScene) || !myXmlAsset){
						if(EditorUtility.DisplayDialog("Empty fields!!", "Please fill the correct values", "OK")){}
					}else
					if(!myXmlAsset){
						if(EditorUtility.DisplayDialog("XML file is empty", "Please select a file", "OK")){}
					}else{
						if(EditorUtility.DisplayDialog("MUST DELETE ALL Created AREAS IN SCENE!!", "ARE YOU SURE;", "YES" , "NO")){
							ImportAreas.CreateSceneAreas(myScenesNames[epilegmeniSkini], myXml, 0);
						}
					}

				}

				
//				if(epilegmeniSkini<myScenesNames.Length && !isPreviewMode){
					GUILayout.Label ("", EditorStyles.boldLabel);
					if(GUILayout.Button("Export All")){
						CreateAreaOnMap.ExportAllAreasPaths();
					}
//				}
				
				
				
//				if(epilegmeniSkini<myScenesNames.Length && isPreviewMode){
					GUILayout.Label ("", EditorStyles.boldLabel);
					if(GUILayout.Button("RESET")){
						if(EditorUtility.DisplayDialog("All data will be lost!", "are you sure?", "OK", "Cancel")){
							myXmlAsset=null;
							movementXml=null;
							epilegmeniSkini=0;
							Array.Clear(myScenesNames,0,myScenesNames.Length);
							ImportAreas.DestroyAreas();
							EditorApplication.NewScene();
						}
					}
//				}


			}
			#endregion
			
		}
		
	}

	string loadedScene;

	void LoadScene(){
		if(menuXML==null){
			menuXML = new XmlDocument();
			
			TextAsset textAsset = (TextAsset) Resources.Load("XML/menu");
			string excludedComments = Regex.Replace(textAsset.text, "(<!--(.*?)-->)", string.Empty);
			menuXML.LoadXml(excludedComments);

		}

		XmlNodeList sceneList = menuXML.SelectNodes ("/menu/sceneAreas/sceneArea");

		foreach (XmlNode scene in sceneList) 
		{
			if(scene ["periods"] != null)
			{
				if (scene ["periods"].ChildNodes.Count > 0)
				{
					XmlNodeList periods = scene ["periods"].ChildNodes;												 
					foreach (XmlNode period in periods)
					{
						if (period ["poiName"] != null)
						{
							if (period ["poiName"].InnerText == myScenesNames[epilegmeniSkini]) {
								if (period ["sceneName"] != null)
								{
									if (period ["sceneName"].InnerText != "") {
										loadedScene = period ["sceneName"].InnerText;
										EditorApplication.OpenScene("Assets/Scenes/"+loadedScene+".unity");
									} 
								}
							}
						}
					}
				}
			}
		}

	}

	#endregion

	#region GRAPHIC_3D
	void _Graphic_3D(){
		GUILayout.Label ("", EditorStyles.boldLabel);
		
		// Create a selection grid where the user can select the current type
		__currentType = EditorGUILayout.Popup("Choose XML",__currentType, __typeStrings);
		
		if(prevType!=__currentType){
			// Load the current xml
			string path = "Assets/Resources/XML/" + __typeStrings[__currentType] + ".xml";
			myXmlAsset =  Resources.LoadAssetAtPath(path, typeof(TextAsset)) as TextAsset;
			prevType=__currentType;
		}

		GUILayout.Label ("Or assign the xml with movement parameters", EditorStyles.boldLabel);
		myXmlAsset = (TextAsset) EditorGUILayout.ObjectField ("Movement XML", myXmlAsset, typeof(TextAsset), false);

		if(myXmlAsset)
		{
			if(movementXml==null){
				myXml = myXmlAsset.name;
				
				movementXml = new XmlDocument(); 
				
				string excludedComments = Regex.Replace(myXmlAsset.text, "(<!--(.*?)-->)", string.Empty);
				movementXml.LoadXml(excludedComments);
				
				XmlNodeList myScenes = movementXml.FirstChild.ChildNodes;
				
				myScenesNames = new string[myScenes.Count];
				
				if(myScenes.Count>0){
					for(int i=0; i<myScenes.Count; i++){
						myScenesNames[i] = myScenes[i].Name;
					}
				}
				epilegmeniSkini=0;
				
			}else{
				if(myXmlAsset.name!=myXml){
					movementXml=null;
					epilegmeniSkini=0;
					Array.Clear(myScenesNames,0,myScenesNames.Length);
				}
			}

			#region check if xml is the correct
			if(movementXml!=null)
			{
				//read gps ref point
				XmlNode sceneArea = movementXml.SelectNodes ("/movement/" + myScenesNames[epilegmeniSkini] + "/sceneArea")[0];
				if(sceneArea==null){
					
					if(EditorUtility.DisplayDialog("Select the name of XML that is contains all map info!", "Select movement.xml", "OK")){
						myXmlAsset=null;
						movementXml=null;
						epilegmeniSkini=0;
						Array.Clear(myScenesNames,0,myScenesNames.Length);
					}
				}
			}
			#endregion
			
			
			GUILayout.Label ("", EditorStyles.boldLabel);
			epilegmeniSkini = EditorGUILayout.Popup("Select Scene", epilegmeniSkini, myScenesNames);
			
			if(epilegmeniSkini<myScenesNames.Length && !isPreviewMode){
				GUILayout.Label ("", EditorStyles.boldLabel);
				if(GUILayout.Button("Preview Map")){
					Preview(true);
				}
			}


			
			if(epilegmeniSkini<myScenesNames.Length && isPreviewMode){
				GUILayout.Label ("", EditorStyles.boldLabel);
				if(GUILayout.Button("RESET")){
					if(EditorUtility.DisplayDialog("All data will be lost!", "are you sure?", "OK", "Cancel")){
						EditorApplication.NewScene();
						isPreviewMode=false;
					}
				}
//				GUILayout.Label ("", EditorStyles.boldLabel);
//				if(GUILayout.Button("Export Areas")){
//					if(EditorUtility.DisplayDialog("All data will be lost!", "are you sure?", "OK", "Cancel")){
//						EditorApplication.NewScene();
//						isPreviewMode=false;
//					}
//				}
			}
			
		}else{
			if(movementXml!=null){
				movementXml=null;
			}
		}
	}
	#endregion

	#region MANY_POINTS
	void _Many_Points(){
		myPoints = EditorGUILayout.IntSlider ("Area Points", myPoints, 2, 20);
		
		myAreaHeight = EditorGUILayout.Slider ("Area Height", myAreaHeight, 0f, 100f);
		
		myRadius = EditorGUILayout.Slider ("Area Radius", myRadius, 1f, 30f);
		
		colorStart = EditorGUILayout.ColorField("start point color",colorStart);
		colorPoint = EditorGUILayout.ColorField("points color",colorPoint);
		
		GUILayout.Label ("", EditorStyles.boldLabel);
		if(GUILayout.Button("Make New Area")){
			myCreatedAreas++; 
			CreateNewArea.MakeAreaPoints(myPoints,myAreaHeight,myRadius,colorStart,colorPoint,SceneView.lastActiveSceneView.pivot);
		}
	}
	#endregion

	#region CUBE
	void _Cube(){
		myAreaHeight = EditorGUILayout.Slider ("Area Height", myAreaHeight, 0f, 100f);
		myRadius = EditorGUILayout.Slider ("Area Side Size", myRadius, 1f, 30f);
		colorStart = EditorGUILayout.ColorField("Area Color",colorStart);
		
		GUILayout.Label ("", EditorStyles.boldLabel);
		if(GUILayout.Button("Make Square Area")){
			CreateNewArea.CreateCubeArea(myRadius,myAreaHeight,colorStart,SceneView.lastActiveSceneView.pivot);
		}
		
		GUILayout.Label ("", EditorStyles.boldLabel);
		if(GUILayout.Button("Create Points for all Square created Areas")){
			myCreatedAreas++;
			CreateNewArea.SetSfairesPoints(false);
		}
	}
	#endregion

	#region COLLIDERS
	void _Colliders(){
		GUILayout.Label ("", EditorStyles.boldLabel);
		GUILayout.Label ("Select an object with childs \nand boxcollider components\nto convert them to square areas", EditorStyles.boldLabel);
		targetObject = (GameObject) EditorGUILayout.ObjectField ("Target father", targetObject, typeof(GameObject), true);
		
		if(targetObject)
		{
			addAreaSize = EditorGUILayout.IntSlider ("Add to every area size", addAreaSize, 0, 20);
			
			if(GUILayout.Button("Convert BoxColliders to Dead Areas")){
				if(EditorUtility.DisplayDialog("For every boxcollider in childs it will be create an area", "ARE YOU SURE;", "YES" , "NO")){
					CreateNewArea.CreateAreasFromBoxColliders(targetObject, addAreaSize);
				}
			}
		}
	}
	#endregion

	#region GRAPHIC
	void _Graphic(){
		GUI.contentColor = Color.yellow;
		helpGraphics = EditorGUILayout.Foldout(helpGraphics, "HELP");
		
		if(helpGraphics)
		{
			GUILayout.Box("Assign the texture2d you created with the dots\nRead/Write must be enabled and format must be \nRGBA32 or ARGB32 at import settings of the texture", EditorStyles.boldLabel);
		}
		
		GUI.contentColor = Color.white;
		
		areasTexture = (Texture2D) EditorGUILayout.ObjectField ("Texure with dots", areasTexture, typeof(Texture2D), false);
		
		
		if(areasTexture)
		{
			if(areasTexture.format!=TextureFormat.RGBA32 && areasTexture.format!=TextureFormat.ARGB32){
				if(EditorUtility.DisplayDialog("Wrong format for texture [" + areasTexture.name + "]", "Change format to be RGBA32 or ARGB32 at import settings of the texture [" + areasTexture.name + "]", "OK" )){
					Debug.LogWarning("format is "+areasTexture.format);
					areasTexture=null;
					return;
				}
			}
			
			try
			{
				areasTexture.GetPixel(0, 0);
			}
			catch(UnityException e)
			{
				if(e.Message.StartsWith("Texture '" + areasTexture.name + "' is not readable"))
				{
					if(EditorUtility.DisplayDialog("Please enable read/write on texture [" + areasTexture.name + "]", "Texture will be remove", "OK" )){
						Debug.LogWarning("Please enable read/write on texture [" + areasTexture.name + "]");
						areasTexture=null;
						return;
					}
				}
			}
			
			if(!isTextureOK)
			{
				GUILayout.Label ("", EditorStyles.boldLabel);
				
				if(GUILayout.Button("Check Texture"))
				{
					dotDedomena.Clear();
					blueDots=-1;
					blackDots=-1;
					redDots=-1;
					greenDots=-1;
					
					int notTransparencyPixels = 0;
					
					for (int x = 0; x < areasTexture.width; x++) 
					{
						for (int y = 0; y < areasTexture.height; y++) 
						{
							Color c = areasTexture.GetPixel (x, y);
							if (c.a > 0.1f)
							{
								notTransparencyPixels++;
								
								//										float r = c.r *255f;
								//										float g = c.g *255f;
								//										float b = c.b *255f;
								//										float a = c.a *255f;
								//
								//										Debug.Log(r+"/"+g+"/"+b+"/"+a);
								
								if(notTransparencyPixels>1000){
									if(EditorUtility.DisplayDialog("Texture Error!!!", "Transparency pixels small amount.", "OK")){
										areasTexture=null;
										dotDedomena.Clear();
										return;
									}
								}
								
								//get data
								dotData myData = new dotData();
								myData.Pos = new Vector2(x,y);
								myData.Xroma = myXroma(c);
								
								dotDedomena.Add(myData);
							}
						}
					}
					
					if(dotDedomena.Count<=2){
						if(EditorUtility.DisplayDialog("Texture Error!!!", "Colored Dots small amount.", "OK")){
							areasTexture=null;
							dotDedomena.Clear();
							return;
						}
					}
					
					Debug.LogWarning("dedomena = "+dotDedomena.Count);
					showdotsAmounts=true;
					isTextureOK=true;
				}
			}else
				if(isTextureOK)
			{
				if(blackDots==-1 && blueDots==-1 && redDots==-1 && greenDots==-1)
				{
					blackDots = dotDedomena.FindAll(x => x.Xroma == Color.black).Count;
					blueDots = dotDedomena.FindAll(x => x.Xroma == Color.blue).Count;
					redDots = dotDedomena.FindAll(x => x.Xroma == Color.red).Count;
					greenDots = dotDedomena.FindAll(x => x.Xroma == Color.green).Count;
				}
				
				//						Debug.LogWarning("black dots are "+bl);
				//						Debug.LogWarning("red dots are "+rd);
				//						Debug.LogWarning("green dots are "+gr);
				//						Debug.LogWarning("blue dots are "+blu);
				
				//						EditorGUI.HelpBox(new Rect(myWindow.position.min.x,myWindow.position.height,100f,100f), "asfdsdafasdf", MessageType.Info);
				showdotsAmounts = EditorGUILayout.Foldout(showdotsAmounts, "Texture Data Results");
				
				if(showdotsAmounts)
				{
					GUI.contentColor = Color.yellow;
					GUILayout.Box("black dots = "+blackDots+"\n"+"red dots = "+redDots+"\n"+"green dots = "+greenDots+"\n"+"blue dots = "+blueDots, EditorStyles.boldLabel);
				}
				GUI.contentColor = Color.white;
				
				GUILayout.Label ("", EditorStyles.boldLabel);
				GUILayout.Label ("Assign the xml with movement parameters", EditorStyles.boldLabel);
				myXmlAsset = (TextAsset) EditorGUILayout.ObjectField ("Movement XML", myXmlAsset, typeof(TextAsset), false);
				
				if(myXmlAsset)
				{
					if(movementXml==null){
						myXml = myXmlAsset.name;
						
						movementXml = new XmlDocument(); 
						
						string excludedComments = Regex.Replace(myXmlAsset.text, "(<!--(.*?)-->)", string.Empty);
						movementXml.LoadXml(excludedComments);
						
						XmlNodeList myScenes = movementXml.FirstChild.ChildNodes;//SelectNodes(movementXml.Name);
						//							Debug.LogWarning(myScenes.Count);
						
						myScenesNames = new string[myScenes.Count];
						
						if(myScenes.Count>0){
							for(int i=0; i<myScenes.Count; i++)
							{
								//									Debug.LogWarning(fc.Name);
								myScenesNames[i] = myScenes[i].Name;
							}
							
							
						}
						epilegmeniSkini=0;
						
					}else{
						if(myXmlAsset.name!=myXml){
							movementXml=null;
							epilegmeniSkini=0;
							Array.Clear(myScenesNames,0,myScenesNames.Length);
						}
					}
					
					
					GUILayout.Label ("", EditorStyles.boldLabel);
					epilegmeniSkini = EditorGUILayout.Popup("Select Scene", epilegmeniSkini, myScenesNames);
					
					if(epilegmeniSkini<myScenesNames.Length){
						GUILayout.Label ("", EditorStyles.boldLabel);
						if(GUILayout.Button("Preview Map")){
							//									Debug.LogWarning(myScenesNames[epilegmeniSkini]);
							Preview(false);
						}
					}
					
					if(epilegmeniSkini<myScenesNames.Length){
						GUILayout.Label ("", EditorStyles.boldLabel);
						if(GUILayout.Button("Export Areas")){
							//										Debug.LogWarning(myScenesNames[epilegmeniSkini]);
							ExportDedomena();
						}
					}
					
				}else{
					if(movementXml!=null){
						movementXml=null;
					}
				}
			}
			
			
		}else{
			isTextureOK = false;
		}
	}
	#endregion

	#region CIRCLE
	void _Circle(){
		myAreaHeight = EditorGUILayout.Slider ("Area Height", myAreaHeight, 0f, 100f);
		myRadius = EditorGUILayout.Slider ("Area Radius", myRadius, 1f, 30f);
		colorStart = EditorGUILayout.ColorField("Area Color",colorStart);
		
		GUILayout.Label ("", EditorStyles.boldLabel);
		if(GUILayout.Button("Make New Circle Area")){
			CreateNewArea.CreateCircleArea(myRadius,myAreaHeight,colorStart,SceneView.lastActiveSceneView.pivot);
		}
		
		GUILayout.Label ("", EditorStyles.boldLabel);
		if(GUILayout.Button("Create Points for all Circle created Areas")){
			myCreatedAreas++;
			CreateNewArea.SetSfairesPoints(true);
		}
	}
	#endregion

	#endregion

	#region GRAPHICAL FUNCTIONS

	int epilegmeniSkini = 0;
	bool showdotsAmounts,helpGraphics;
	string[] myScenesNames;
	List<dotData> dotDedomena = new List<dotData>();
	bool isTextureOK = false;
	int blueDots, blackDots, redDots, greenDots;

	struct dotData{
		public Color Xroma{get; set;}
		public Vector2 Pos{get; set;}
	}

	Color myXroma(Color c)
	{

		Vector4 d = new Vector4(c.r * 255f ,c.g *255f, c.b *255f, c.a *255f);
		//black
		if(d.x==d.y && d.x==d.z && d.w>50f)
		{
			return Color.black;
		}else
		//red
		if(d.x > d.y && d.x>d.z && d.w>50f)
		{
			return Color.red;
		}else
		//green
		if(d.y>d.x && d.y>d.z && d.w>50f)
		{
			return Color.green;
		}else
		//blue
		if(d.z>d.x && d.z>d.y && d.w>50f)
		{
			return Color.blue;
		}

		return Color.clear;
	}

	List<dotData> blackDedomena = new List<dotData>();
	List<dotData> redDedomena = new List<dotData>();
	List<dotData> greenDedomena = new List<dotData>();
	List<dotData> blueDedomena = new List<dotData>();

	void ExportDedomena(){ 															Debug.Log("ExportDedomena");
		if(dotDedomena.Count>0 && movementXml!=null && areasTexture!=null)
		{
			//read gps ref point
			XmlNode sceneArea = movementXml.SelectNodes ("/movement/" + myScenesNames[epilegmeniSkini] + "/sceneArea")[0];

			if(!string.IsNullOrEmpty(sceneArea["posX"].InnerText) && !string.IsNullOrEmpty(sceneArea["posZ"].InnerText)){
				float posX = float.Parse(sceneArea["posX"].InnerText);
				float posZ = float.Parse(sceneArea["posZ"].InnerText);
				moveSettings.posCenterOfMap=new Vector2(posX,posZ);
			}

			XmlNode settings = movementXml.SelectSingleNode ("/movement/" + myScenesNames[epilegmeniSkini] + "/settings");
			moveSettings.terrainSize = new Vector2(float.Parse(settings["terrainSize"]["x"].InnerText),float.Parse(settings["terrainSize"]["y"].InnerText));
			
			blackDedomena.Clear();
			redDedomena.Clear();
			greenDedomena.Clear();
			blueDedomena.Clear();

			blackDedomena = dotDedomena.FindAll(b => b.Xroma==Color.black);
			redDedomena = dotDedomena.FindAll(b => b.Xroma==Color.red);
			greenDedomena = dotDedomena.FindAll(b => b.Xroma==Color.green);
			blueDedomena = dotDedomena.FindAll(b => b.Xroma==Color.blue);

			Create(Mode.AREA, blackDedomena);
		}
	}

	enum Mode{AREA,PATH,DEAD_AREA}
	Mode mode = Mode.AREA;
	List<cArea> activeAreas = new List<cArea>();
	List<GameObject> pointsObjects = new List<GameObject>();

	void Create(Mode myMode, List<dotData> myData)
	{
		switch(myMode)
		{
			case(Mode.AREA):
				if(myData.Count>0)
				{
					GameObject tt = new GameObject("SQUARE_AREA");
					tt.transform.position = Vector3.zero;
					
//					Debug.LogWarning("filter texture = "+areasTexture.width+" X "+areasTexture.height);
					activeAreas.Clear();
					cArea area = new cArea();
					List<Vector3> simeia = new List<Vector3>();
					pointsObjects.Clear();


					for(int i=0; i<myData.Count; i++)
					{
						float posX = myData[i].Pos.x;
						float posY = myData[i].Pos.y;

//						Debug.Log(myData[i].Pos);
						
						float normalPointX = posX / areasTexture.width;// - (tex.width/2f);
						float normalPointY = posY / areasTexture.height;// - (tex.height/2f);
						
						normalPointX *= areasTexture.width;// 1024f; //mapFilterRect.rect.width;
						normalPointY *= areasTexture.height;// 1024f;	   //mapFilterRect.rect.height;
						
						Vector2 normPoint = new Vector2(normalPointX, normalPointY);

//						Debug.Log("normPoint = "+normPoint);
						
						Vector2 rectPoint = normPoint + new Vector2(-0.5f * areasTexture.width, -0.5f * areasTexture.height); //(-512f, -512f); //mapFilterRect.rect.min;

//						Debug.Log("rectPoint = "+rectPoint);
//						Debug.Log("zoomLogos = "+zoomLogos());
						
						//get person position
						Vector3 blackPos = Vector3.zero;

						//calculate 2d pos to world position in scene
						blackPos.x = rectPoint.x / (zoomLogos ().x );///2f);
						blackPos.y = 0f;
						blackPos.z = rectPoint.y / (zoomLogos ().y);// /2f) ;
						
						blackPos.x += moveSettings.posCenterOfMap.x;
						blackPos.z += moveSettings.posCenterOfMap.y;
						
						if(i==0){
							GameObject gb = new GameObject("start");
							
							gb.transform.parent = tt.transform;
							gb.transform.localPosition = Vector3.zero;
							
							gb.transform.position = blackPos;

						pointsObjects.Add(gb);
						}else{
							GameObject gb = new GameObject("point");
							
							gb.transform.parent = tt.transform;
							gb.transform.localPosition = Vector3.zero;
							
							gb.transform.position = blackPos;
							pointsObjects.Add(gb);
						}

						simeia.Add(blackPos);
					}

					

					area.Simeia=Stathis.AreaTools.ExportAreaSimeia(pointsObjects);
					activeAreas.Add(area);

					Debug.Log(activeAreas.Count+" "+area.Simeia.Count);
				}
				break;
			case(Mode.DEAD_AREA):
				
				break;
			case(Mode.PATH):
				
				break;
			default:
				Debug.LogWarning("Warning!! No Mode is selected...");
				break;

		}

	}

	//stathera's calculations

	Vector2 terrainSize, mapRectSize, stathera, posCenterOfMap;

	/// <summary>
	/// Convertation world to map position and reversed
	/// </summary>
	Vector2 zoomLogos(){
		//get map container width/height
//		mapX = mapRect.sizeDelta.x;
//		mapY = mapRect.sizeDelta.y;
		
		//divide map size with terrain size
		stathera = new Vector2(areasTexture.width / moveSettings.terrainSize.x , areasTexture.height / moveSettings.terrainSize.y);
		
		return stathera;
	}

	#endregion

	#region Auto Params
	string GetSceneName(){
		#if UNITY_5
		return SceneManager.GetActiveScene().name;
		#endif
		
		string[] strScenePathSplit = EditorApplication.currentScene.Split('/');
		string[] strFileWithExtension =
			strScenePathSplit[strScenePathSplit.Length - 1].Split('.');
		string sceneName = strFileWithExtension[0];
		if( sceneName == null ){
			sceneName = "Scene name not Available";
			Debug.LogWarning("Scene name not Available");
		}
		return sceneName;
	}
	string copyright = "@Stathis 2017 - Scene Areas & Paths Tool";
	#endregion


}

#region DrawAreas CLASS

public class DrawAreas:MonoBehaviour {

	public bool previewAreas,closeArea,isPath,isOnsite,isDead, editableAreas;
	public List<Vector3> myDots = new List<Vector3>();
	public List<Vector3> myAreaDots = new List<Vector3>();
	public List<GameObject> myDotObjects = new List<GameObject>();
	public List<cArea> activeAreas = new List<cArea>();
	List<cLineSegment> perimetros = new List<cLineSegment>();

	Color pathOnsiteXroma = new Color( 1f, 0.474f, 0f, 1f );
	Color areaOnsiteXroma = new Color( 0f, 1f, 1f, 1f );

	void OnDrawGizmos(){
		
		if(previewAreas)
		{
			if (myAreaDots.Count > 0) {
				if(!isPath){
					//area
					for (int m=0; m<myAreaDots.Count-1; m++) {
						if(!isOnsite){
							if(!isDead){
								Gizmos.color=Color.green;
							}else{
								Gizmos.color=Color.black;
							}
						}else{
							if(!isDead){
								Gizmos.color = areaOnsiteXroma;
							}else{
								Gizmos.color = Color.gray;
							}
						}
						Gizmos.DrawLine (new Vector3 (myAreaDots [m].x, myAreaDots [m].y, myAreaDots[m].z), new Vector3 (myAreaDots [m + 1].x, myAreaDots [m + 1].y, myAreaDots [m + 1].z));
					}
				//close the loop
//					Gizmos.color=Color.green;
					Gizmos.DrawLine (new Vector3 (myAreaDots [myAreaDots.Count-1].x, myAreaDots [myAreaDots.Count-1].y, myAreaDots [myAreaDots.Count-1].z), new Vector3 (myAreaDots [0].x, myAreaDots [0].y, myAreaDots[0].z));
				}else{

					//path
					for (int m=0; m<myAreaDots.Count-1; m++) {
						if(!isOnsite){
							Gizmos.color=Color.blue;
						}else{
							Gizmos.color = pathOnsiteXroma;
						}

						Gizmos.DrawLine (new Vector3 (myAreaDots [m].x, myAreaDots [m].y, myAreaDots[m].z), new Vector3 (myAreaDots [m + 1].x, myAreaDots [m + 1].y, myAreaDots [m + 1].z));
					}
				}
			}

			if (myDots.Count > 0) {
				for (int m=0; m<myDots.Count-1; m++) {
					Gizmos.color=Color.red;
					Gizmos.DrawLine (new Vector3 (myDots [m].x, 1f, myDots[m].z), new Vector3 (myDots [m + 1].x, 1f, myDots [m + 1].z));
				}
				//close the loop
				if(closeArea){
					Gizmos.color=Color.green;
					Gizmos.DrawLine (new Vector3 (myDots [myDots.Count-1].x, 1f, myDots [myDots.Count-1].z), new Vector3 (myDots [0].x, 1f, myDots[0].z));
				}
			}


//			Debug.Log("OnDrawGizmos 2");
			//show path
//			if(moveSettings.playerPath.Count>0){
//				for (int k=0; k<moveSettings.playerPath.Count; k++) {
//					Gizmos.color=Color.blue;
//					Gizmos.DrawLine(new Vector3(moveSettings.playerPath[k].StartOfLine.x, 1f,moveSettings.playerPath[k].StartOfLine.y) , new Vector3(moveSettings.playerPath[k].EndOfLine.x, 1f,moveSettings.playerPath[k].EndOfLine.y));
//				}
//			}


//			if(perimetros.Count>0){
//				for (int k=0; k<perimetros.Count; k++) {
//					Gizmos.color=Color.blue;
//					Gizmos.DrawLine(new Vector3(perimetros[k].StartOfLine.x, 1f,perimetros[k].StartOfLine.y) , new Vector3(perimetros[k].EndOfLine.x, 1f,perimetros[k].EndOfLine.y));
//				}
//			}
		}

		if(editableAreas){
			if (myDotObjects.Count > 0) {
				if(!isPath){
					//area
					for (int m=0; m<myDotObjects.Count-1; m++) {
						if(!isOnsite){
							if(!isDead){
								Gizmos.color=Color.green;
							}else{
								Gizmos.color=Color.black;
							}
						}else{
							if(!isDead){
								Gizmos.color = areaOnsiteXroma;
							}else{
								Gizmos.color = Color.gray;
							}
						}
						Gizmos.DrawLine (new Vector3 (myDotObjects [m].transform.position.x, myDotObjects [m].transform.position.y, myDotObjects[m].transform.position.z), new Vector3 (myDotObjects [m + 1].transform.position.x, myDotObjects [m + 1].transform.position.y, myDotObjects [m + 1].transform.position.z));
					}
					//close the loop
					//					Gizmos.color=Color.green;
					Gizmos.DrawLine (new Vector3 (myDotObjects [myDotObjects.Count-1].transform.position.x, myDotObjects [myDotObjects.Count-1].transform.position.y, myDotObjects [myDotObjects.Count-1].transform.position.z), new Vector3 (myDotObjects [0].transform.position.x, myDotObjects [0].transform.position.y, myDotObjects[0].transform.position.z));
				}else{
					
					//path
					for (int m=0; m<myDotObjects.Count-1; m++) {
						if(!isOnsite){
							Gizmos.color=Color.blue;
						}else{
							Gizmos.color = pathOnsiteXroma;
						}
						
						Gizmos.DrawLine (new Vector3 (myDotObjects [m].transform.position.x, myDotObjects [m].transform.position.y, myDotObjects[m].transform.position.z), new Vector3 (myDotObjects [m + 1].transform.position.x, myDotObjects [m + 1].transform.position.y, myDotObjects [m + 1].transform.position.z));
					}
				}
			}
		}
	}

}

#endregion

