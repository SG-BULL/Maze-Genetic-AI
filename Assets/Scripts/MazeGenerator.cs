using System; // je hoeft niet alle "using" dingen hieronder te hebben, als je error hebt bij eentje, dan die weghalen.
using System.Collections;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using UnityEngine.UIElements;
using DG.Tweening;
using UnityEngine.AI;
using System.Security.Cryptography;
using TMPro;
using System.ComponentModel;
using System.Threading;
using Unity.VisualScripting;
using Palmmedia.ReportGenerator.Core.Parser.Analysis;
using UnityEngine.UI;
using Unity.VisualScripting.FullSerializer;
using UnityEngine.SceneManagement;
using System.Linq.Expressions;



public class MazeGenerator : MonoBehaviour
{
    // Start is called before the first frame update

    void Awake(){
        UnityEngine.Random.InitState(100); // seed
        DOTween.Init();
    }

    IEnumerator Start(){
        int getRandomNumber(float num1, float num2){
            float randomValue = UnityEngine.Random.Range(num1, num2);
            int roundedInt = Mathf.RoundToInt(randomValue);
            return roundedInt;
        }

        void giveColor(GameObject obj, Color col){
            MeshRenderer renderer = obj.GetComponent<MeshRenderer>();
            Material newMaterial = new Material(Shader.Find("Standard")); 
            newMaterial.color = col; 
            renderer.material = newMaterial;
        }

        int gridSize = 33; // maze grootte
        Vector3 spawnPos = Vector3.zero; // wordt later geset
        Vector3 endPos = Vector3.zero; // wordt later geset
        
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.transform.position = new Vector3(gridSize/2, -.5f, gridSize/2);
        floor.transform.localScale = new Vector3(gridSize*2, 1, gridSize*2);
        floor.name = "Floor";
        giveColor(floor, Color.black);

        Dictionary<Vector3, Dictionary<object, object>> gridDict = new Dictionary<Vector3, Dictionary<object, object>>();

        Vector3 NEXTPOS = Vector3.zero;

        for (int i = 0; i < gridSize; i++){ // grid maken, een 2D iets, dus dubbel for loop, kijk op line 63 hoe de position eruit ziet
            for (int j = 0; j < gridSize; j++){
                Dictionary<object, object> cubeDict = new Dictionary<object, object>(); // cubeDict gaat in gridDict op line 56. voor elk vierkantje een cubeDict. informatie over vierkantje staat erin, zoals position, canmakepath, etc.
                Vector3 pos = new Vector3(i + .5f, .5f, j + .5f); // verder op de line hierboven: het heet cubeDict, omdat er eerst cubes spawnden op de positie, worden later destroyed
                cubeDict.Add("Position", pos);
                cubeDict.Add("Name", "Cube_" + (1 + i*gridSize + j));
                cubeDict.Add("CanMakePath", true);
            
                if ((i == 0 || i == gridSize-1) && j == (gridSize - 1)/2){ // entrance of exit maken (gele en roze part)
                    bool isEntrance = i == 0;

                    cubeDict.Add(isEntrance ? "IsEntrance" : "IsExit", true);
                    cubeDict["Name"] =  isEntrance ? "Cube_Entrance" : "Cube_Exit";

                    int mul = isEntrance ? 1 : -1;
                    Dictionary<object, object> cubeDictSpawn = new Dictionary<object, object>();
                    Vector3 targetPos = pos - new Vector3(mul,0,0);

                    cubeDictSpawn.Add("Position", targetPos);
                    cubeDictSpawn.Add("Name", isEntrance ? "SpawnLocation" : "EndLocation");
                    cubeDictSpawn.Add(isEntrance ? "IsSpawn" : "IsEnd", true);
                    cubeDictSpawn.Add("CanMakePath", false);

                    if (isEntrance){
                        spawnPos = targetPos;  
                    } else{
                        endPos = targetPos;
                    }
                   
                    gridDict.Add(targetPos, cubeDictSpawn);
                }

                if (i == 0 || i == gridSize-1 || j == 0 || j == gridSize-1 ){ // heeft geen nut nu, laat zien dat de cube aan de rand is van de maze
                    cubeDict.Add("IsBorder", true);
                }

                gridDict.Add(pos, cubeDict); // cubeDict toevoegen
            }
        }

        foreach ((Vector3 pos, Dictionary<object, object> dict) in gridDict){ // kleuren geven aan objects
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = pos;
            cube.name = (string)dict["Name"];
            
            if (dict.ContainsKey("IsEntrance") || dict.ContainsKey("IsExit")) {
                giveColor(cube, dict.ContainsKey("IsEntrance") ? Color.red : Color.green);
            }       

            if (dict.ContainsKey("IsBorder") &&! (dict.ContainsKey("IsEntrance") || dict.ContainsKey("IsExit"))) {
                giveColor(cube, Color.gray);
            }

            if (dict.ContainsKey("IsSpawn") || dict.ContainsKey("IsEnd")) {
                giveColor(cube, dict.ContainsKey("IsSpawn") ? Color.yellow : Color.magenta);

                cube.transform.localScale = new Vector3(1, 0.1f, 1);
                cube.transform.position -= new Vector3(0, .495f, 0);
            }

            dict.Add("Cube", cube);
        }

        Vector3[] dirArr = new Vector3[] { // later voor for loop 4 richtingen op
            new Vector3(-1,0,0), 
            new Vector3(1,0,0), 
            new Vector3(0,0,-1), 
            new Vector3(0,0,1), 
        }; 

        Vector3 UNIT(Vector3 toThis, Vector3 fromThis){ // maakt een vector richting fromthis tothis die een lengte heeft van 1. handig zodat je zelf de lengte kan bepalen door te vermenigvuldigen 
            return (toThis-fromThis).normalized;
        }	 

        Dictionary<Vector3, List<GameObject>> wallDict = new Dictionary<Vector3, List<GameObject>>(); 

        List<Vector3> dirHistoryList = new List<Vector3>();

        void generatePathOnPos(object param){ // pad genereren
            Vector3 pos = (Vector3)param;
            Dictionary<object, object> cubeDict = gridDict[pos];

            if ((bool)cubeDict["CanMakePath"]) {
                for (int i = 0; i < 4; i++) { // 4 muren maken in het begin om een cube (vierkant in de grid) heen

                    void makeWall(){
                        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube); 
                        Vector3 targetPos; 
                        float mul = (i % 2 == 0) ? .5f : -.5f; 

                        if (i == 0 || i == 1){ 
                            targetPos = pos + new Vector3(0,0,mul); 
                            wall.transform.position = targetPos; 
                            wall.transform.localScale = new Vector3(1.1f, 1, .1f); 
                        }else{ 
                            targetPos = pos + new Vector3(mul,0,0); 
                            wall.transform.position = targetPos; 
                            wall.transform.localScale = new Vector3(.1f, 1, 1.1f); 
                        } 

                        if (! wallDict.ContainsKey(targetPos)) { 
                            List<GameObject> wallList = new List<GameObject>(); 
                            wallList.Add(wall); 
                            wallDict.Add(targetPos, wallList); 
                        }else{ 
                            List<GameObject> wallList = wallDict[targetPos]; 
                            wallList.Add(wall); 
                        }

                        wall.name = "Wall_" + i + " " + ((GameObject)cubeDict["Cube"]).name;
                        wall.AddComponent<BoxCollider>(); // boxcollider voor raycasten, raycasts staan niet in deze code
                    }

                    makeWall();
                } 
            }

            if (cubeDict.ContainsKey("Cube")){ 
                Destroy((GameObject)cubeDict["Cube"]); // cube destroyen    
                 cubeDict.Remove("Cube");  

                if (cubeDict.ContainsKey("FromNeighbourPos")){ // fromneighbourpos is om te weten van welke richting je kwam naar de huidige position, dan weet je welke muur je moet destroyen, want er wordt bovenin deze method 4 muren gemaakt, bij een bepaalde kant moeten die destroyed worden, daarom fromneighbourpos
                    Vector3 neighPos = (Vector3)cubeDict["FromNeighbourPos"]; 
                    List<GameObject> wallList =  wallDict[pos + UNIT(neighPos, pos)*.5f]; 
                
                    foreach(GameObject wall in wallList){ // hier dus muren destroyen door fromneighbourpos
                      Destroy(wall); 
                    }

                    wallList.RemoveAll(item => item == null);
                    wallList.RemoveAll(item => item is GameObject);
                } 
            }
           
            cubeDict["CanMakePath"] = false; 

            List<Vector3> dirList = new List<Vector3>(); 

            foreach(Vector3 dir in dirArr){ 
                Vector3 targetPos = pos + dir; 

                if (gridDict.ContainsKey(targetPos) && (bool)gridDict[targetPos]["CanMakePath"]){ 
                    dirList.Add(dir);
                }     
            } 

            int neighCnt = dirList.Count; 

            if (neighCnt > 0){ 
                int index = getRandomNumber(0f, (float)neighCnt-1);

                Vector3 finalDir = dirList[index]; 
                Vector3 nextPos = pos + finalDir; 
                List<GameObject> wallList = wallDict[pos + finalDir *.5f]; 

                dirHistoryList.Add(finalDir); // kijk line 227

                foreach(GameObject wall in wallList) {  // wall destroyen waar je naartoe gaat
                    Destroy(wall); 
                }

                wallList.RemoveAll(item => item == null);
                wallList.RemoveAll(item => item is GameObject);

                gridDict[nextPos].Add("FromNeighbourPos", pos); // dit is belangrijk (bij line 181 ook), de dictionary van volgende position krijgt fromneighbourpos, dat is de huidige positie in de huidige method call, maar dat is de vorige positie in de volgende method call
                generatePathOnPos(nextPos); 
            }else{
                int lastIndex = dirHistoryList.Count - 1; // dirhistorylist is een list met de geschiedenis van alle richtingen die zijn genomen om een pad te generaten

                if (lastIndex < 0) return; // er worden positions removed op line 232 uit de dirhistorylist, als er dus geen neighbours zijn waar een pad op generate kan worden (line 208), return dan als list size onder 0 is, want dan is het klaar

                Vector3 prevDir = dirHistoryList[lastIndex];
                dirHistoryList.RemoveAt(lastIndex);

                NEXTPOS = pos - prevDir; // backtracken, NEXTPOS wordt bij line 250 gebruikt in een while loop om continu te backtracken zonder stack overflow error door recursion 
                return;
            }
        } 

        foreach((Vector3 pos, Dictionary<object, object> dict) in gridDict){ 
            if (dict.ContainsKey("IsEntrance")) { 
                generatePathOnPos(pos); // vanaf de entrance beginnen, dirhistorylistcount wordt groter, dat is nodig voor de while loop bij line 248, deze code stopt bij line 235, dan gaat de code verder naar de while loop
                break; 
            } 
        } 

        int LASTINDEX = dirHistoryList.Count - 1;

        while (LASTINDEX >= 0){
            LASTINDEX = dirHistoryList.Count - 1;
            generatePathOnPos(NEXTPOS);
        };

        yield return new WaitForSeconds(1f); // 1 seconde wachten, kan veel korter, zodat er genoeg tijd is voor de muren om te spawnen, was nodig later voor raycasten, want je kan niet meteen raycasten, eerst wachten totdat de muren er zijn
        
        foreach((Vector3 pos, Dictionary<object, object> dict) in gridDict) { // walls bij de exit en entrance removen
            if (dict.ContainsKey("IsEntrance") || dict.ContainsKey("IsExit")) { 

                float mul = dict.ContainsKey("IsEntrance") ? .5f : -.5f; 
                List<GameObject> wallList = wallDict[pos - new Vector3(mul,0,0)]; 

                foreach(GameObject wall in wallList){ 
                    Destroy(wall); 	 
                } 

                wallList.RemoveAll(item => item == null);
                wallList.RemoveAll(item => item is GameObject);
            }  
        } 
    }

    // Update is called once per frame
    void Update()
    {

    }
}
