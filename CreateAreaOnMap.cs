using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEditor;
using eChrono;
using System.Xml;
using System.Text.RegularExpressions;
using System.IO;
using System.Text;

[CustomEditor(typeof(AreaGroup))]
public class CreateAreaOnMap : Editor {

	private static bool drawMode = false; // When we are in edit mode (Raycast mode)
	//private static int __count = 0; // Counter of the placed items (Not really used right now)
	private GameObject __objectGroup; // GameObject containing the group of the anchorPoints group.
	private int __currentType = 0;
	private static string[] __typeStrings;
	string myXml;
	int prevType=0;
	GameObject dot,map;
	DrawAreas drA , currentArea;
	GameObject currAreaObject;
	int countingIndex;

	void OnEnable()
	{
		map = GameObject.Find("map");
		if(!map){Debug.LogWarning("No Map!!!");}else{drA=map.GetComponent<DrawAreas>(); drA.previewAreas=true;}
		points.Clear();
	}


	string drawTitle(int val){
		if(val==1){
			return "CREATING OFFSITE AREA";
		}else
		if(val==2){
			return "CREATING ONSITE AREA";
		}else
		if(val==3){
			return "CREATING OFFSITE PATH";
		}else
		if(val==4){
			return "CREATING ONSITE PATH";
		}else
		if(val==5){
			return "CREATING OFFSITE DEAD AREA";
		}else
		if(val==6){
			return "CREATING ONSITE DEAD AREA";
		}

		return "Creating Null!!!";
	}


	List<GameObject> points = new List<GameObject>();

	int pick=0;

	GUIStyle currentStyle = null;

	void InitStyles()
	{
		if( currentStyle == null )
		{
			currentStyle = new GUIStyle( GUI.skin.box );
			currentStyle.normal.background = MakeTex( 2, 2, new Color( 0f, 1f, 0f, 0.5f ) );
		}
	}
	
	Texture2D MakeTex( int width, int height, Color col )
	{
		Color[] pix = new Color[width * height];
		for( int i = 0; i < pix.Length; ++i )
		{
			pix[ i ] = col;
		}
		Texture2D result = new Texture2D( width, height );
		result.SetPixels( pix );
		result.Apply();
		return result;
	}

	#region ONSCENEGUI

	void OnSceneGUI()
	{

		InitStyles();

		#region SCENE VIEW LABEL
		Handles.BeginGUI();
		
		GUILayout.BeginArea(new Rect(20, 20, 350, 300));
		GUI.backgroundColor = Color.black;

		var rect = EditorGUILayout.BeginVertical();
		
		GUI.color = Color.white;
		GUI.Box(rect,"",currentStyle);// GUIContent.none);
		
//		GUI.color = Color.white;

		if(currAreaObject!=null){
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			GUILayout.Label(drawTitle(pick)+"\nA -> ENABLE DRAW MODE\nZ -> ADD NEW POINT AT MOUSE POSITION\nW -> MOVE LAST POINT AT MOUSE POSITION\nX -> DELETE POINT ONE BY ONE\n       STARTING FROM THE LAST");
			GUILayout.FlexibleSpace();
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
		}

		#region BUTTONS
		
		GUI.color = Color.white;
		
		GUILayout.BeginHorizontal();
		GUI.backgroundColor = Color.green;
		
		if (GUILayout.Button("New OffSite Area")) {
			NewArea(true,1);
			pick=1;
		}

		if (GUILayout.Button("New OnSite Area")) {
			NewArea(false,1);
			pick=2;
		}
		
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUI.backgroundColor = Color.blue;
		
		if (GUILayout.Button("New Offsite Path")) {
			NewArea(true,3);
			pick=3;
		}

		if (GUILayout.Button("New Onsite Path")) {
			NewArea(false,3);
			pick=4;
		}

		
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUI.backgroundColor = Color.black;
		
		if (GUILayout.Button("New OffSite Dead Area")) {
			NewArea(true,2);
			pick=5;
		}
		
		if (GUILayout.Button("New Onsite Dead Area")) {
			NewArea(false,2);
			pick=6;
		}
		
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		GUI.backgroundColor = Color.gray;
		
		if (GUILayout.Button("Export All")) {
			if(EditorUtility.DisplayDialog("All Paths and Areas will be exported!!", "Are you sure?", "Yes", "No")){
				ExportAllAreasPaths();
			}
		}
		GUILayout.EndHorizontal();

		#endregion
		
		EditorGUILayout.EndVertical();
		
		
		GUILayout.EndArea();
		
		Handles.EndGUI();

		#endregion

		if(Event.current.type == EventType.keyDown && Event.current.keyCode == KeyCode.A && currAreaObject!=null){
			drawMode=!drawMode;
		}
		// If we are in edit mode and the user clicks (right click, middle click or alt+left click)
		if (drawMode)
		{
			if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Z && Event.current.type != EventType.keyUp)
			{
				if(currAreaObject==null){return;}

				// Shoot a ray from the mouse position into the world
				Ray worldRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
				RaycastHit hit;

				if (Physics.Raycast(worldRay, out hit, Mathf.Infinity))
				{
					if(hit.transform && hit.transform.name=="map")
					{
						countingIndex++;
						dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
						dot.transform.localScale = new Vector3(0.3f,0.1f,0.3f);
						// Place the prefab at correct position (position of the hit).
						dot.transform.position = hit.point;
						dot.transform.parent = currAreaObject.transform;// hit.transform;
						// Mark the instance as dirty because we like dirty
						EditorUtility.SetDirty(dot);

						points.Add(dot);
						if(countingIndex<=1){
							dot.name="start";
						}else{
							dot.name="point";
						}

						if(drA){
							drA.myDots.Add(dot.transform.position);
							currentArea.myAreaDots.Add(dot.transform.position);
							currentArea.myDotObjects.Add(dot);
							currentArea.myDots.Add(dot.transform.position);
							drA.myDotObjects.Add(dot);
						}
					}
				}
			}else
			if(Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.W && Event.current.type != EventType.keyUp){
				//if not exist find last in the list
				if(!dot){
					if(points.Count>0){
						int d = points.Count-1;
						dot = points[d];
					}else{
						return;
					}

					if(!dot){
						return;
					}
				}
				// Shoot a ray from the mouse position into the world
				Ray worldRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
				RaycastHit hit;
				
				if (Physics.Raycast(worldRay, out hit, Mathf.Infinity))
				{
					if(hit.transform && hit.transform.name=="map")
					{
						if(dot){
							dot.transform.position = hit.point;
						}
					}

					int p = drA.myDotObjects.IndexOf(dot);
					drA.myDots[p] = dot.transform.position;
					currentArea.myAreaDots[p] = dot.transform.position;
					p = currentArea.myDotObjects.IndexOf(dot);
					currentArea.myDotObjects[p].transform.position = dot.transform.position;
				}
			}else
			if(Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.X && Event.current.type != EventType.keyUp){

//				countingIndex--;

//				if(currentArea.myDotObjects.Count>0){
//					int indx = currentArea.myDotObjects.Count-1;
//					dot = currentArea.myDotObjects[indx];
//					
//					if(dot){
//						currentArea.myDotObjects.Remove(dot);
//						currentArea.myDots.RemoveAt(indx);
//						currentArea.myAreaDots.RemoveAt(indx);
//					}
//				}

				if(drA.myDotObjects.Count>0){
					countingIndex--;

					int indx = drA.myDotObjects.Count-1;
					dot = drA.myDotObjects[indx];

					if(dot){
						Debug.LogWarning(currentArea.myDotObjects.Count);
						Debug.LogWarning(currentArea.myDots.Count);
						drA.myDotObjects.Remove(dot);
						drA.myDots.RemoveAt(indx);
						currentArea.myAreaDots.RemoveAt(indx);
						currentArea.myDotObjects.Remove(dot);
						currentArea.myDots.RemoveAt(indx);
						points.RemoveAt(indx);
						DestroyImmediate(dot);
					}
				}


			}
			// Mark the event as used
			Event.current.Use();
		} // End if __editMode
	} // End OnSceneGUI

	#endregion

	#region OnInspectorGUI
	
	public override void OnInspectorGUI()
	{
		// Toggle edit mode
		if (drawMode)
		{// If we are in editing mode, make the button green and change the label
			GUI.color = Color.green;
			if (GUILayout.Button("Disable Editing")) { drawMode = false; }
		}
		else
		{
			GUI.color = Color.red; // Normal color if w're not in editing mode
			if (GUILayout.Button("Enable Editing"))
			{
//				drawMode = true;
//				// Get the objectGroup (Active selection)
//				__objectGroup = Selection.activeGameObject;
			}
		}
		GUI.color = Color.white;
	}

	#endregion

	#region Create New
	/// New Creation
	/// select if onsite or offsite
	/// type -> 
	/// 1 = Area
	/// 2 = Dead
	/// 3 = Path
	void NewArea(bool isOffsite, int type){
		//check if is empty
		if(currAreaObject!=null){
			Transform[] t = currAreaObject.GetComponentsInChildren<Transform>();
			if(t.Length<=1){
				if(EditorUtility.DisplayDialog("YOU HAVE AN EMPTY AREA IN SCENE..", "ADD POINTS!", "OK")){
					Debug.LogWarning("YOU HAVE AN EMPTY AREA IN SCENE.. ADD POINTS");
				}
				return;
			}
		}

		if(drA){
			drA.myDots.Clear();
			drA.myDotObjects.Clear();
		}

		if(currentArea){
			currentArea.myDots.Clear();
			currentArea.previewAreas=true;
		}

		currAreaObject = new GameObject();

		currentArea = currAreaObject.AddComponent<DrawAreas>();

		if(type==1)
		{
			if(isOffsite){
				currAreaObject.name="Area_OffSite";
			}else{
				currAreaObject.name="Area_OnSite";
				currentArea.isOnsite=true;
			}
		}else
		if(type==2)
		{
			currentArea.isDead=true;
			if(isOffsite){
				currAreaObject.name="Dead_OffSite";
			}else{
				currAreaObject.name="Dead_OnSite";
				currentArea.isOnsite=true;
			}
		}else
		if(type==3)
		{
			currentArea.isPath=true;
			if(isOffsite){
				currAreaObject.name="PathOffsite";
			}else{
				currAreaObject.name="PathOnsite";
				currentArea.isOnsite=true;
			}
		}



		currAreaObject.transform.position = Vector3.zero;
//		currentArea = new cArea();
		countingIndex=0;
	}


	#endregion
	
	#region EXPORT AREAS And PATHS
	
	public static void ExportAllAreasPaths(){
		List<GameObject> perioxesOffSite = new List<GameObject>();
		List<GameObject> perioxesOnSite = new List<GameObject>();
		List<GameObject> deadOffSite = new List<GameObject>();
		List<GameObject> deadOnSite = new List<GameObject>();
		List<GameObject> monopatiaOffSite = new List<GameObject>();
		List<GameObject> monopatiaOnSite = new List<GameObject>();
		List<GameObject> perioxisPoints = new List<GameObject>();
		List<GameObject> pathPoints = new List<GameObject>();
		GameObject[] gs = FindObjectsOfType(typeof(GameObject)) as GameObject[];

		foreach(GameObject g in gs){
			if(g.name=="Area_OffSite"){
				perioxesOffSite.Add(g);
			}else
			if(g.name=="Area_OnSite"){
				perioxesOnSite.Add(g);
			}else
			if(g.name=="Dead_OffSite"){
				deadOffSite.Add(g);
			}else
			if(g.name=="Dead_OnSite"){
				deadOnSite.Add(g);
			}else
			if(g.name=="PathOffsite"){
				monopatiaOffSite.Add(g);
			}else
			if(g.name=="PathOnsite"){
				monopatiaOnSite.Add(g);
			}
		}

		//Start righting the xml
		string areaTxt = "Areas_&_Paths.txt";
		TextWriter sw = new StreamWriter(areaTxt);


		#region Areas

		for(int t=0; t<4; t++)
		{
			List<GameObject> myAreas = new List<GameObject>();

			if(t==0){
				myAreas = perioxesOffSite;
				//if no areas dont write anything - skip
				if(myAreas.Count>0){
					sw.Write(xmlHeaderOffSiteAreas());
				}else{continue;}
			}else
			if(t==1){
				myAreas = perioxesOnSite;
				//if no areas dont write anything - skip
				if(myAreas.Count>0){
					sw.Write(xmlHeaderOnSiteAreas());
				}else{continue;}
			}else
			if(t==2){
				myAreas = deadOffSite;
				//if no areas dont write anything - skip
				if(myAreas.Count>0){
					sw.Write(xmlHeaderOffSiteDeadAreas());
				}else{continue;}
			}else
			if(t==3){
				myAreas = deadOnSite;
				//if no areas dont write anything - skip
				if(myAreas.Count>0){
					sw.Write(xmlHeaderOnSiteDeadAreas());
				}else{continue;}
			}


			for(int i=0; i<myAreas.Count; i++)
			{
				GameObject b= myAreas[i];
				DrawAreas areaScript =b.GetComponent<DrawAreas>();

				//onoma monopatiou me noumero
				b.name = "exported_"+b.name + "_" + i.ToString();
				string areaName =(i+1).ToString() ;

				perioxisPoints.Clear();
				perioxisPoints = areaScript.myDotObjects;
				
//				Debug.LogWarning("perioxisPoints = "+perioxisPoints.Count);

				for(int w=0; w<2; w++)
				{

					if(w==0){
						if(areaScript.isDead){
							sw.Write(lineStartDeadArea(areaName));
						}else{
							sw.Write(lineStartArea(areaName));
						}
					}else if(w==1){
						sw.Write(lineStartPerimetros(areaName));
					}

					int num=perioxisPoints.Count;

					for (int a=0; a<num; a++){
						perioxisPoints[a].transform.position=new Vector3(Mathf.Round(perioxisPoints[a].transform.position.x *100f)/100f, perioxisPoints[a].transform.position.y, Mathf.Round(perioxisPoints[a].transform.position.z * 100f)/100f);
					}
					
					sw.Flush();
					
					if(w==0){
						for (int z=0; z<num; z++){
							sw.WriteLine(xmlTransform(perioxisPoints[z]));
						}
						sw.Write(lineEndPoint(areaName));
						
					}else 
					if(w==1){
						for (int a=0;a<num-1;a++){
							sw.Write(xmlTransformStart(perioxisPoints[a]));
							sw.Flush();
							sw.Write(xmlTransformEnd(perioxisPoints[a+1]));
							sw.Flush();
						}
						sw.Write(xmlTransformStart(perioxisPoints[perioxisPoints.Count-1]));
						sw.Flush();
						sw.Write(xmlTransformEnd(perioxisPoints[0]));
						sw.Flush();
						sw.Write(lineEndPerimetros(areaName));
						
					}
					
				}

				if(areaScript.isDead){
					sw.Write(lineEndDeadArea(areaName));
				}else{
					sw.Write(lineEndArea(areaName));
				}
			}

			if(t==0){
				sw.Write(xmlFooterOffSiteAreas());
			}else
			if(t==1){
				sw.Write(xmlFooterOnSiteAreas());
			}else
			if(t==2){
				sw.Write(xmlFooterOffSiteDeadAreas());
			}else
			if(t==3){
				sw.Write(xmlFooterOnSiteDeadAreas());
			}

			sw.Write(lineKeno());
			
			sw.Flush();

			if(myAreas.Count==1){
				Debug.Log (myAreas.Count+" περιοχη εξήχθη με επιτυχία !");
			}else{
				Debug.Log (myAreas.Count+" περιοχες εξήχθησαν με επιτυχία !");
			}

		}

		#endregion

		#region PATHS

		for(int s=0; s<2; s++)
		{
			List<GameObject> myPaths = new List<GameObject>();
			
			
			if(s==0){
				myPaths = monopatiaOffSite;
				//if no paths dont write anything - skip
				if(myPaths.Count>0){
					sw.Write(xmlHeaderOffSitePaths());
				}else{
					continue;
				}
			}else
			if(s==1){
				myPaths = monopatiaOnSite;
				//if no paths dont write anything - skip
				if(myPaths.Count>0){
					sw.Write(xmlHeaderOnSitePaths());
				}else{
					continue;
				}
			}


			//paths
			for(int i=0; i<myPaths.Count; i++)
			{
				DrawAreas areaScript = myPaths[i].GetComponent<DrawAreas>();
				
				//onoma monopatiou me noumero
				myPaths[i].name = "exported_"+myPaths[i].name + "_" + i.ToString();

				pathPoints.Clear();
				pathPoints = areaScript.myDotObjects;

				int num=pathPoints.Count;
				
				for (int a=0; a<num; a++){
					pathPoints[a].transform.position=new Vector3(Mathf.Round(pathPoints[a].transform.position.x *100f)/100f, pathPoints[a].transform.position.y, Mathf.Round(pathPoints[a].transform.position.z * 100f)/100f);
				}
				
				sw.Flush();
					
				for (int a=0;a<num-1;a++){
					sw.Write(xmlTransformStartPath(pathPoints[a]));
					sw.Flush();
					sw.Write(xmlTransformEndPath(pathPoints[a+1]));
					sw.Flush();
				}
						
					
				
			}
			
			if(s==0){
				sw.Write(xmlFooterOffSitePaths());
			}else
			if(s==1){
				sw.Write(xmlFooterOnSitePaths());
			}
			
			sw.Write(lineKeno());
			
			sw.Flush();
			
			if(myPaths.Count==1){
				Debug.Log (myPaths.Count+" monopati εξήχθη με επιτυχία !");
			}else{
				Debug.Log (myPaths.Count+" monopatia εξήχθησαν με επιτυχία !");
			}

		}

		#endregion

		sw.Close ();
		
		//anoikse to arxeio
		Application.OpenURL(areaTxt);
		
	}
	
	#endregion

	#region TXT Areas WRITER

	static string lineKeno(){
		StringBuilder s = new StringBuilder();
		s.AppendLine(" ");
		return s.ToString();	
	}

	static string xmlTransformStart(GameObject go){
		Transform t = go.transform;
		StringBuilder s = new StringBuilder();
		if(go.name=="startWithLimit"){
			s.AppendLine("<segment limits=\"on\">");
		}else{
			s.AppendLine("<segment limits=\"off\">");
		}
		go.name="start";
		s.AppendLine("<start name=\"" + go.name + "\"" + " x=\"" + t.position.x + "\" y=\"" + t.position.z  + "\" />");			
		return s.ToString();
	}
	
	static string xmlTransformEnd(GameObject go){
		Transform t = go.transform;
		StringBuilder s = new StringBuilder();
		s.AppendLine("<finish name=\"" + go.name + "\"" + " x=\"" + t.position.x + "\" y=\"" + t.position.z  + "\" />");	
		s.AppendLine("</segment>");	
		return s.ToString();
	}
	
	static string lineStartArea(string onomaArea){
		StringBuilder s = new StringBuilder();
		s.AppendLine(" ");
		s.AppendLine("<!--____________________ start of Area "+onomaArea+" ____________________-->");
		s.AppendLine("<activeArea status="+"\"on\""+">");
		s.AppendLine("<name>"+"Active Area "+onomaArea+"</name>");
		s.AppendLine("<points>");
		return s.ToString();	
	}

	static string lineStartDeadArea(string onomaArea){
		StringBuilder s = new StringBuilder();
		s.AppendLine(" ");
		s.AppendLine("<!--____________________ start of Dead Area "+onomaArea+" ____________________-->");
		s.AppendLine("<deadArea status="+"\"on\""+">");
		s.AppendLine("<name>"+"Dead Area "+onomaArea+"</name>");
		s.AppendLine("<points>");
		return s.ToString();	
	}
	
	static string lineEndPoint(string onomaArea){
		StringBuilder s = new StringBuilder();
		s.AppendLine("</points>");
		return s.ToString();	
	}
	
	static string lineStartPerimetros(string onomaArea){
		StringBuilder s = new StringBuilder();
		s.AppendLine("<perimetros>");
		return s.ToString();	
	}
	
	static string lineEndPerimetros(string onomaArea){
		StringBuilder s = new StringBuilder();
		s.AppendLine("</perimetros>");
		return s.ToString();	
	}
	
	static string lineEndArea(string onomaArea){
		StringBuilder s = new StringBuilder();
		s.AppendLine("</activeArea>");
		s.AppendLine("<!--____________________ end of Area "+onomaArea+" ____________________-->");
		s.AppendLine(" ");
		return s.ToString();	
	}

	static string lineEndDeadArea(string onomaArea){
		StringBuilder s = new StringBuilder();
		s.AppendLine("</deadArea>");
		s.AppendLine("<!--____________________ end of Dead Area "+onomaArea+" ____________________-->");
		s.AppendLine(" ");
		return s.ToString();	
	}
	
	static string xmlTransform(GameObject go){
		StringBuilder s = new StringBuilder();	
		s.AppendLine("<point " + " X=\"" + go.transform.position.x + "\"" + " Y=\"0\"" + " Z=\"" + go.transform.position.z  + "\" />");			
		return s.ToString();
	}
	
	
	static string xmlHeaderOffSiteAreas(){
		StringBuilder s = new StringBuilder();
		s.AppendLine("<ActiveAreas>");
		return s.ToString();	
	}
	
	static string xmlFooterOffSiteAreas(){
		StringBuilder s = new StringBuilder();
		s.AppendLine("</ActiveAreas>");
		return s.ToString();	
	}

	static string xmlHeaderOnSiteAreas(){
		StringBuilder s = new StringBuilder();
		s.AppendLine("<ActiveAreasOnSite>");
		return s.ToString();	
	}
	
	static string xmlFooterOnSiteAreas(){
		StringBuilder s = new StringBuilder();
		s.AppendLine("</ActiveAreasOnSite>");
		return s.ToString();	
	}

	static string xmlHeaderOffSiteDeadAreas(){
		StringBuilder s = new StringBuilder();
		s.AppendLine("<DeadSpots>");
		return s.ToString();	
	}
	
	static string xmlFooterOffSiteDeadAreas(){
		StringBuilder s = new StringBuilder();
		s.AppendLine("</DeadSpots>");
		return s.ToString();	
	}
	
	static string xmlHeaderOnSiteDeadAreas(){
		StringBuilder s = new StringBuilder();
		s.AppendLine("<DeadSpotsOnSite>");
		return s.ToString();	
	}
	
	static string xmlFooterOnSiteDeadAreas(){
		StringBuilder s = new StringBuilder();
		s.AppendLine("</DeadSpotsOnSite>");
		return s.ToString();	
	}

	#endregion

	#region TXT Paths WRITER

	static string lineStartPath(string onomaPath){
		StringBuilder s = new StringBuilder();
		s.AppendLine(" ");
		s.AppendLine("<!--____________________ start of "+onomaPath+" ____________________-->");
		s.AppendLine("<path>");
		return s.ToString();	
	}
	
	static string lineEndPath(string onomaPath){
		StringBuilder s = new StringBuilder();
		s.AppendLine("</path>");
		s.AppendLine("<!--____________________ end of "+onomaPath+" ____________________-->");
		s.AppendLine(" ");
		return s.ToString();	
	}
	
	static string xmlTransformPath(GameObject go){
		StringBuilder s = new StringBuilder();	
		s.AppendLine("<point name=\"" + go.name + "\"" + " X=\"" + go.transform.position.x + "\"" + " Y=\"0\"" + " Z=\"" + go.transform.position.z  + "\" />");			
		return s.ToString();
	}
	
	
	static string xmlHeaderOffSitePaths(){
		StringBuilder s = new StringBuilder();
		s.AppendLine("<path>");
		return s.ToString();	
	}
	
	static string xmlFooterOffSitePaths(){
		StringBuilder s = new StringBuilder();
		s.AppendLine("</path>");
		return s.ToString();	
	}

	static string xmlHeaderOnSitePaths(){
		StringBuilder s = new StringBuilder();
		s.AppendLine("<pathOnSite>");
		return s.ToString();	
	}
	
	static string xmlFooterOnSitePaths(){
		StringBuilder s = new StringBuilder();
		s.AppendLine("</pathOnSite>");
		return s.ToString();	
	}
	
	
	static string xmlTransformStartPath(GameObject go){
		Transform t = go.transform;
		StringBuilder s = new StringBuilder();
		if(go.name=="startNoLimit"){
			s.AppendLine("<segment limits=\"off\">");
		}else{
			s.AppendLine("<segment limits=\"on\">");
		}
		go.name="start";
		s.AppendLine("<start name=\"" + go.name + "\"" + " x=\"" + t.position.x + "\" y=\"" + t.position.z  + "\" />");			
		return s.ToString();
	}
	
	static string xmlTransformEndPath(GameObject go){
		Transform t = go.transform;
		StringBuilder s = new StringBuilder();
		s.AppendLine("<finish name=\"" + go.name + "\"" + " x=\"" + t.position.x + "\" y=\"" + t.position.z  + "\" />");	
		s.AppendLine("</segment>");	
		return s.ToString();
	}

	#endregion


}
