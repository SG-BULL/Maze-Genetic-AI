using System;
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
using System.Diagnostics;


public class MazeAndGeneticAIWithComments : MonoBehaviour
{
    // Start is called before the first frame update

    // public Text generationText; 
    // public Text populationText;
    // public Text totalTimeText;

    //float totalTime = 0;
    bool endReached = false;

    // yourTextLabel.text = newText;

    System.Random random = new System.Random();

    void Awake(){
        UnityEngine.Random.InitState(100); // seed
        DOTween.Init();
    }

    IEnumerator Start(){
        // for (int i = 0; i < 10; i++)
        // {
        //     float randomValue = UnityEngine.Random.value;
        //     Debug.Log("Random Value: " + randomValue);
        // }


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


        
        int gridSize = 30;
        Vector3 spawnPos = Vector3.zero; // wordt later geset
        Vector3 endPos = Vector3.zero; // wordt later geset
        
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.transform.position = new Vector3(gridSize/2, -.5f, gridSize/2);
        floor.transform.localScale = new Vector3(gridSize*2, 1, gridSize*2);
        floor.name = "Floor";
        giveColor(floor, Color.black);

        Dictionary<Vector3, Dictionary<object, object>> gridDict = new Dictionary<Vector3, Dictionary<object, object>>();

        Vector3 NEXTPOS = Vector3.zero;

        for (int i = 0; i < gridSize; i++){
            for (int j = 0; j < gridSize; j++){
                Dictionary<object, object> cubeDict = new Dictionary<object, object>();
                Vector3 pos = new Vector3(i + .5f, .5f, j + .5f);
                cubeDict.Add("Position", pos);
                cubeDict.Add("Name", "Cube_" + (1 + i*gridSize + j));
                cubeDict.Add("CanMakePath", true);
            
                if ((i == 0 || i == gridSize-1) && j == (gridSize - 1)/2){
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

                if (i == 0 || i == gridSize-1 || j == 0 || j == gridSize-1 ){
                    cubeDict.Add("IsBorder", true);
                }

                gridDict.Add(pos, cubeDict);
            }
        }

        foreach ((Vector3 pos, Dictionary<object, object> dict) in gridDict){
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



            
        Vector3[] dirArr = new Vector3[] { 
            new Vector3(-1,0,0), 
            new Vector3(1,0,0), 
            new Vector3(0,0,-1), 
            new Vector3(0,0,1), 
        }; 

        Vector3 UNIT(Vector3 toThis, Vector3 fromThis){ 
            // Vector3 unit = toThis-fromThis;
            // unit = unit.normalized;

            return (toThis-fromThis).normalized;
           // return unit;
        }	 

        float MAG(Vector3 pos1, Vector3 pos2){
            return Vector3.Distance(pos1, pos2);
        }

        Dictionary<Vector3, List<GameObject>> wallDict = new Dictionary<Vector3, List<GameObject>>(); 

        List<Vector3> dirHistoryList = new List<Vector3>();

        void generatePathOnPos(object param){ 
            Vector3 pos = (Vector3)param;
            Dictionary<object, object> cubeDict = gridDict[pos];

            if ((bool)cubeDict["CanMakePath"]) {
                for (int i = 0; i < 4; i++) { 

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
                        wall.AddComponent<BoxCollider>();
                    }

                    makeWall();

              //     UnityMainThreadDispatcher.Instance.Enqueue(makeWall);
                } 
            }

            if (cubeDict.ContainsKey("Cube")){
                Destroy((GameObject)cubeDict["Cube"]); 
                 cubeDict.Remove("Cube");  

                if (cubeDict.ContainsKey("FromNeighbourPos")){ 
                    Vector3 neighPos = (Vector3)cubeDict["FromNeighbourPos"]; 
                    List<GameObject> wallList =  wallDict[pos + UNIT(neighPos, pos)*.5f]; 
                
                    foreach(GameObject wall in wallList){ 
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

      
         //   yield return new WaitForSeconds(1f);





            if (neighCnt > 0){ 
                int index = getRandomNumber(0f, (float)neighCnt-1);
               // print(index);
                Vector3 finalDir = dirList[index]; 
                Vector3 nextPos = pos + finalDir; 
                List<GameObject> wallList = wallDict[pos + finalDir *.5f]; 

                dirHistoryList.Add(finalDir);
        	 //   Debug.Log(wallList.Count);

                foreach(GameObject wall in wallList) { 
                    Destroy(wall); 
                }

                wallList.RemoveAll(item => item == null);
                wallList.RemoveAll(item => item is GameObject);
                

                gridDict[nextPos].Add("FromNeighbourPos", pos); 
                generatePathOnPos(nextPos); 
                // Thread newThread = new Thread(new ParameterizedThreadStart(generatePathOnPos));
                // newThread.Start(nextPos);

            }else{ // random punt kiezen in de map om verder te generaten 
                   // muren destroyen in een random richting, want dit is een situatie waarin de positie die gekozen is in het midden zit tussen walls 
                
                //backtracken
                int lastIndex = dirHistoryList.Count - 1;

                if (lastIndex < 0) return;

            //    Debug.Log(lastIndex);

                Vector3 prevDir = dirHistoryList[lastIndex];
                dirHistoryList.RemoveAt(lastIndex);

                NEXTPOS = pos - prevDir;
                return;


                // Thread newThread = new Thread(new ParameterizedThreadStart(generatePathOnPos));
                // newThread.Start(pos - prevDir);

              //  generatePathOnPos(pos - prevDir);

                // als er al een pad is gemaakt bij een bepaalde position, dan geen walls maken en niks destroyen

                   // dirList = new List<Vector3>(); 

                // foreach(Vector3 dir in dirArr) { 
                //     // check of het niet een entrance of exit part is, en mag ook niet end en spawnlocation zijn, want je checkt canMakePath niet, dus moet je deze individueel checken.	

                //     Vector3 targetPos = pos + dir; 

                //     if (gridDict.ContainsKey(targetPos) ){ 
                //         Dictionary<object, object> targetCubeDict = gridDict[targetPos]; 

                //         if (targetCubeDict != null &&! targetCubeDict.ContainsKey("IsEntrance") &&! targetCubeDict.ContainsKey("IsExit")){ 
                //             dirList.Add(dir); 
                //         } 
                //     } 
                // }

                // neighCnt = dirList.Count; 
                // Vector3 finalDir = dirList[random.Next(0, neighCnt)]; 
                // List<GameObject> wallList = wallDict[pos + finalDir*.5f];

                // foreach(GameObject wall in wallList){ 
                //      Destroy(wall); 
                // } 

                // wallList.RemoveAll(item => item == null);

                // foreach((Vector3 vPos, Dictionary<object, object> dict) in gridDict){ 
                //     if ((bool)dict["CanMakePath"]) { // is dit wel goed?
                //         generatePathOnPos(vPos); 
                //     } 
                // } 



                // in een list opslaan welke richtingen de script heeft genomen
                // want hij moet backtracken en niet random dingen doen
                // alleen de dir opslaan wanneer er succesvol een nieuw pad is gemaakt
                // dan als je backtracked de negative dir pakken
                // dan steeds weer checken
            }
        } 

        
        // oke nu doe ik weer een random position kiezen, maar dat heeft nadelen 
        // die gast in die video deed backtracken 
        // kan het ook gewoon met random position kiezen? 
        // ja kan denk ik wel, er is een situatie waarin er 4 neighbours zijn die paden zijn maar dat de random gekozen part nog niks is, dus dan kies je een random richting 
        // en breek je daar de muur 


        foreach((Vector3 pos, Dictionary<object, object> dict) in gridDict){ 
            if (dict.ContainsKey("IsEntrance")) { 
                generatePathOnPos(pos); 
                // Thread newThread = new Thread(new ParameterizedThreadStart(generatePathOnPos));
                // newThread.Start(pos);
                break; 
            } 
        } 

        int LASTINDEX = dirHistoryList.Count - 1;

        while (LASTINDEX >= 0){
            LASTINDEX = dirHistoryList.Count - 1;
          //  print(LASTINDEX);
            generatePathOnPos(NEXTPOS);
          //  yield return null;
        };




        yield return new WaitForSeconds(1f);

        // op het eind kan ik de muren bij de exit en de entrance destroyen 

        foreach((Vector3 pos, Dictionary<object, object> dict) in gridDict) { 
            if (dict.ContainsKey("IsEntrance") || dict.ContainsKey("IsExit")) { 
                // muur destroyen bij de juiste kant, geen idee of het nou de z as positief of negatief 

                float mul = dict.ContainsKey("IsEntrance") ? .5f : -.5f; 
                List<GameObject> wallList = wallDict[pos - new Vector3(mul,0,0)]; 

                foreach(GameObject wall in wallList){ 
                    Destroy(wall); 	 
                } 

                wallList.RemoveAll(item => item == null);
                wallList.RemoveAll(item => item is GameObject);
            }  
        } 

        

        // maze meer richtingen geven: 

        // foreach((Vector3 pos, Dictionary<object,object> cubeDict) in gridDict){
        //     if (! cubeDict.ContainsKey("IsSpawn") ||! cubeDict.ContainsKey("IsEnd")) {
        //         List<Vector3> dirList = new List<Vector3>();

        //         foreach(Vector3 dir in dirArr) {
        //             if (gridDict.ContainsKey(pos + dir)){
        //                 List<GameObject> wallList = wallDict[pos + dir*.5f];
        //                 if (wallList.Count == 0){
        //                     dirList.Add(dir);
        //                 }
        //             }
        //         }

        //         if (dirList.Count == 2){
        //             Vector3 dir1 = dirList[0];
        //             Vector3 dir2 = dirList[1];

        //             if (dir1 + dir2 != new Vector3(0,0,0)){
        //                 // elke richting behalve dir1 en dir2, en check eerst of er wel een gridposition is aan de kant wan de walls die je wilt destroyen 

        //                 foreach(Vector3 vDir in dirArr) {
        //                     if(vDir != dir1 && vDir != dir2 && gridDict.ContainsKey(pos + vDir) && random.Next(0,10) == 0){
        //                         List<GameObject> wallList = wallDict[pos + vDir*.5f]; 

        //                         foreach(GameObject wall in wallList){ 
        //                             Destroy(wall); 	 
        //                         } 

        //                         wallList.RemoveAll(item => item == null);
        //                         wallList.RemoveAll(item => item is GameObject);
        //                     }
        //                 }    
        //             }
        //         }
        //     }
        // }

        // yield return new WaitForSeconds(.5f);

        List<Vector3> tweenPosList = new List<Vector3>();

        foreach((Vector3 pos, Dictionary<object,object> cubeDict) in gridDict){
            if (! cubeDict.ContainsKey("IsSpawn") ||! cubeDict.ContainsKey("IsEnd")) {
                List<Vector3> dirList = new List<Vector3>();

                // als dirlist.count gelijk is aan 2 en de 2 directions bij elkaar zijn gelijk aan Vnew(0,0,0) dan geen part maken, anders wel

                foreach(Vector3 dir in dirArr) {
                    if (gridDict.ContainsKey(pos + dir)){
                        List<GameObject> wallList = wallDict[pos + dir*.5f];
                       // Debug.Log(wallList.Count);
                     //  Debug.Log(wallList[0]);
                     //  Debug.Log(wallList[1]);
                        if (wallList.Count == 0){
                          //  Debug.Log("yes");
                            dirList.Add(dir);
                        }
                    }
                }

                //Debug.Log(dirList.Count);

                if (dirList.Count == 2){
                  //  Debug.Log("cunt");
                    Vector3 dir1 = dirList[0];
                    Vector3 dir2 = dirList[1];

                   // Debug.Log(dir1 + dir2);

                    if (dir1 + dir2 != new Vector3(0,0,0)){
                        // GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        // cube.transform.position = pos - new Vector3(0, .490f, 0);
                        // cube.transform.localScale = new Vector3(1, .1f, 1);
                        // giveColor(cube, Color.blue);

                        tweenPosList.Add(pos);
                    }

                }else{
                    // GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    // cube.transform.position = pos - new Vector3(0, .490f, 0);
                    // cube.transform.localScale = new Vector3(1, .1f, 1);
                    // giveColor(cube, Color.blue);

                    tweenPosList.Add(pos);
                }   
            }
        }

        yield return new WaitForSeconds(.5f); // 1 seconde wachten, want blijkbaar is die wall nog niet destroyed bij de entrance, waardoor de makeagent function niet goed werkt
        Dictionary<int, Dictionary<Vector3, Vector3>> allDeterminedPosDict = new Dictionary<int, Dictionary<Vector3, Vector3>>();

        LayerMask layerMask = LayerMask.GetMask("Agent");

        int popSiz = 50;
        int genCnt = 1;
      

        List<float> fitnessList = new List<float>();
        Dictionary<float, Dictionary<Vector3,Vector3>> fitnessDeterminedPosDict = new Dictionary<float, Dictionary<Vector3,Vector3>>();
        Dictionary<float, Dictionary<object,object>> fitnessAgentDict = new Dictionary<float, Dictionary<object,object>>();
        
       // bool makeBestDeterminedPosDict = false;

        // Dictionary<Vector3,Vector3> makeDeterminedPosDict2(Vector3 agentPos){
        //     Dictionary<Vector3,Vector3> determinedPosDict = new Dictionary<Vector3,Vector3>();
        
        //     foreach(Vector3 _ in tweenPosList){
        //         foreach(Vector3 pos in tweenPosList){

        //             List<Vector3> potentialPosList = new List<Vector3>();

        //             if ((pos.x == agentPos.x || pos.z == agentPos.z) &&  pos != agentPos){
        //                 potentialPosList.Add(pos);
        //             } 
            
        //             List<Vector3> removeList = new List<Vector3>();

        //             foreach(Vector3 vPos in potentialPosList){
        //                 if (Physics.Raycast(agentPos, UNIT(vPos, agentPos), out RaycastHit hit, MAG(pos, agentPos), ~layerMask)){
        //                     removeList.Add(vPos);
        //                 }
        //             }

        //             foreach(Vector3 vPos in removeList){
        //                 potentialPosList.Remove(vPos);
        //             }
            
        //             Dictionary<Vector3, object> closestPosDict = new Dictionary<Vector3, object>();
        //             // direction en position, position kan nil zijn, dat betekent dat er niks is voor die direction

        //             foreach(Vector3 dir in dirArr){
        //                 closestPosDict.Add(dir, null);
        //             }

        //             foreach(Vector3 vPos in potentialPosList){
        //                 Vector3 dir = UNIT(vPos, agentPos);
        //                 float closestDist = closestPosDict[dir] != null ? MAG((Vector3)closestPosDict[dir], agentPos) : float.PositiveInfinity;
        //                 if (MAG(vPos, agentPos) < closestDist) {
        //                     closestPosDict[dir] = vPos;
        //                 }
        //             }

        //             List<Vector3> finalPosList = new List<Vector3>();
        //             foreach((Vector3 dir, object vPos) in closestPosDict) {
        //                 if (vPos != null &&! determinedPosDict.ContainsKey((Vector3)vPos)) {
        //                     finalPosList.Add((Vector3)vPos);
        //                 }
        //             }

        //             if (finalPosList.Count > 0){
        //                 Vector3 finalPos = finalPosList[getRandomNumber(0f, (float)finalPosList.Count-1)];
        //                 determinedPosDict.Add(agentPos, finalPos);
        //             //  agentPos = finalPos;
        //             }    

        //             // reversen, dus dat je niet vanuit de agentpos kijkt, maar juist kijkt vanaf een random pos
        //             // vanaf die random pos kies je een van de dichtsbijzijnde tweenposses
        //             // die random tweenpos is de key en de random pos dan de value

        //             // er klopt geen poep van want dan is er backtracking
        //             // je kan dit fixen door terwijl de agent aan het navigeren is een if statement te doen ofzo, maar ja. 

        //            // agentPos = pos; // dit gaat niet werken denk ik

        //            // terug naar oude systeem? 
        //         }
        //     }

        //     allDeterminedPosDict.Add(allDeterminedPosDict.Count, determinedPosDict);
        //     return determinedPosDict;
        // }

        void makeDeterminedPosDict(Vector3 agentPos, Dictionary<Vector3, Vector3> determinedPosDict){
             List<Vector3> makeFinalPosList(Vector3 vAgentPos){
                    //Vector3 agentPos = agent.transform.position;
                List<Vector3> potentialPosList = new List<Vector3>();

                // TODO: voor elke position in tweenposlist vooraf een keuze maken 

                foreach(Vector3 pos in tweenPosList){
                    if ((pos.x == vAgentPos.x || pos.z == vAgentPos.z) &&  pos != vAgentPos){
                        potentialPosList.Add(pos);
                    } 
                }

        //        print(potentialPosList.Count);

                List<Vector3> removeList = new List<Vector3>();

                foreach(Vector3 pos in potentialPosList){
                    if (Physics.Raycast(vAgentPos, UNIT(pos, vAgentPos), out RaycastHit hit, MAG(pos, vAgentPos), ~layerMask)){
                        removeList.Add(pos);
                        // print("wth");
                        // print(hit.collider.gameObject.name);

                        // IEnumerator debugRaycast(){
                        //     while (true) {
                        //         Debug.DrawLine(agentPos, hit.point, Color.red);
                        //         yield return new WaitForSeconds(.1f);
                        //     }
                        // }

                        // StartCoroutine(debugRaycast());
                    }
                }

                foreach(Vector3 pos in removeList){
                    potentialPosList.Remove(pos);
                }


    //                print(potentialPosList.Count);

                Dictionary<Vector3, object> closestPosDict = new Dictionary<Vector3, object>();
                // direction en position, position kan nil zijn, dat betekent dat er niks is voor die direction


                foreach(Vector3 dir in dirArr){
                    closestPosDict.Add(dir, null);
                }

                foreach(Vector3 pos in potentialPosList){
                    Vector3 dir = UNIT(pos, vAgentPos);
                    float closestDist = closestPosDict[dir] != null ? MAG((Vector3)closestPosDict[dir], vAgentPos) : float.PositiveInfinity;
                    if (MAG(pos, vAgentPos) < closestDist) {
                        closestPosDict[dir] = pos;
                    }
                }

                List<Vector3> finalPosList = new List<Vector3>();
                foreach((Vector3 dir, object pos) in closestPosDict) {
                    if (pos != null &&! determinedPosDict.ContainsKey((Vector3)pos)) {
                        finalPosList.Add((Vector3)pos);
                    }
                }

                return finalPosList;
            }
           
            if (fitnessList.Count > 0 && determinedPosDict.Count == 0) { // laater een andere variable met bool maken om dit te executen
                Dictionary<object,object> bestAgentT = fitnessAgentDict[fitnessList[getRandomNumber(0f, Mathf.Clamp(2f, 1f, (float)fitnessList.Count-1))]]; // top 3
                
                // meerdere agentTs kiezen, dus top 5 ofzo.

                // nieuwe table maken, kan niet zomaar table setten, want dan cloned hij niet de table;
                // nee niet nieuwe table, de table is gewoon determinedposdict
                

                // foreach((Vector3 k, Vector3 v) in (Dictionary<Vector3,Vector3>)bestAgentT["DeterminedPosDict"]){
                //     determinedPosDict[k] = v;
                // }

                // visitedPosList

                List<Vector3> bestVisitedPosList = (List<Vector3>)bestAgentT["VisitedPosList"];
                List<Vector3> targetPosList = new List<Vector3>();
               
                for (int i = bestVisitedPosList.Count - 1; i >= 0; i--){ // van achter naar voor
                    Vector3 pos = bestVisitedPosList[i];

                    int dirCnt = 0; 

                    foreach(Vector3 dir in dirArr){
                        if (wallDict.ContainsKey(pos + dir*.5f)){
                            List<GameObject> wallList = wallDict[pos + dir*.5f];
                            if (wallList.Count == 0){
                                dirCnt++;
                            }
                        }
                    }
                    
                    if (dirCnt >= 3){
                        targetPosList.Add(pos);
                    }
                }

                Vector3 targetPos = new Vector3(.66f,66f,.66f); // gewoon random positie voor geen error

                for(int i = 0; i < targetPosList.Count; i++){ // targetpos kiezen, degene later op het pad hebben meer kans om gekozen te worden
                    if (getRandomNumber(1f, 100f) <= 65 || i == targetPosList.Count - 1){
                        targetPos = targetPosList[i];
                        break;
                    }
                }

                // let op dat ik hieronder wel de bestvisitedposlist direct aanpas

              //  print("reached 1");

                for (int i = bestVisitedPosList.Count - 1; i >= 0; i--){ // van achter naar voor alle posities removen die na de targetpos komen
                    Vector3 pos = bestVisitedPosList[i];
                    
                    if (pos == targetPos){
                        break;
                    } else {
                        bestVisitedPosList.Remove(pos);
                    }
                }

             //   print("reached 2");

                for (int i = 0; i < bestVisitedPosList.Count; i++){
                    Vector3 pos = bestVisitedPosList[i];
                    
                    if (i < bestVisitedPosList.Count-1){
                        determinedPosDict.Add(pos, bestVisitedPosList[i + 1]); 
                    }
                }

                // als het goed is is de targetpos de laatste in de determinedposdict, nu de nieuwe richting vinden ervoor
                // de nieuwe richting is de richting waar de ai niet vandaan is gekomen en die de ai de vorige keer niet heeft gekozen. 
                // trouwens, ik kan het ook zo laten, maar dan is het niet gegarandeerd dat de ai een betere kant op gaat, wat training iets langer laat duren
                // laten we dit even testen. 

                makeDeterminedPosDict(targetPos, determinedPosDict);

                return;
                
                 // hieronder staan alleen nog maar comments



                // ik heb nu de targetpos, wat nu? ik moet alles destroyen wat na de targetposlist komt 


                // deze code hieronder kan vervangen worden met door met een loop te gaan door de visitsedposlist
                     // foreach((Vector3 k, Vector3 v) in (Dictionary<Vector3,Vector3>)bestAgentT["DeterminedPosDict"]){
                //     determinedPosDict[k] = v;
                // }

                


                // ik wil dus in volgorde? hoeft dat? ja in volgorde backtracken
                // dan moet ik inderdaad door visitedposlist en van achter beginnen
                // van achteren naar voren kijken naar punten waar je 3-4 richtingen op kan gaan
                // kijk dus naar de muren
                // dan als dat punt bestaat, in een list storen, die dus van dichtbij naar ver gaat
                // dan kies je hier een punt uit en gaat de script normaal verder met makedeterminedposdict vanaf dat punt

                // je delete dan de rest van de determinedposdict maar hoe?
                // alles deleten dus wat na dat 3-4 richting punt komt, maar behouden wat daarvoor is. 
                // oh nee je kan gewoon door visitedposlict loopen en alle keys eruit halen die die value heeft
                // en stoppen wanneer je de target 3-4 richtingen punt raakt

                // foreach((Vector3 k, Vector3 v) in determinedPosDict){
                //     List<Vector3> finalPosLis = makeFinalPosList(k);
                //     if (finalPosLis.Count > 1) { // niet een corner of dode hoek
                //         Vector3 finalPos = finalPosLis[getRandomNumber(0f, (float)finalPosLis.Count-1)];
                //         determinedPosDict[k] = finalPos;
                //         // je moet verder met finalpos op een of andere manier. 


                //         // eerst bij het punt waar de ai voor de dode hoek koos kijken welke kanten naast de kant van de dode hoek op kan gaan.
                //         // als die er zijn, dan kies je een random kant, die kant mag trouwens ook niet zijn van een position die al eerder is bezocht
                //         // dus dan kan het zijn dat hij 0 kanten op kan gaan, omdat hij zo diep zit in een dode hoek

                //         // ja wat ga je hiertegen doen he? 
                //         // heletijd backtracken? totdat er een punt is waar er een andere richting op gegaan kan worden
                //         // dan moet je vanaf deze positie een nieuwe potential posdict generaten. 

                //         // ik moet misschien heel het systeem veranderen, eerst even de seed fixen. 
                //         // ander systeem is gewoon voor letterlijk elke positie een keuze maken, volgensmij is het nu alleen posities die achterelkaar komen, als een soort pad
                //         // maar als ik letterlijk alle posities heb, dan is het minder moeilijk. 


                //         // oude systeem, dan heletijd backtracken tot een punt waar je 3 of 4 richtingen op kan gaan
                //         // 3 richtingen zijn er, maar je kan in ieder geval niet nog meer terug en ook niet naar de dode hoek richting, dus kan maar 1 kant op
                //         // maar wat als het sub optimaal is en uiteindelijk leidt naar nog een dode hoek, wat dan? 
                //         // its over

                        


                //     }
                // }

                // function maken die de potential posities kiest. 
                
            }


           
        
        
        
            List<Vector3> finalPosList = makeFinalPosList(agentPos);

            if (finalPosList.Count > 0){
                Vector3 finalPos = finalPosList[getRandomNumber(0f, (float)finalPosList.Count-1)];

                determinedPosDict.Add(agentPos, finalPos);
                makeDeterminedPosDict(finalPos, determinedPosDict);
            }else{
                allDeterminedPosDict.Add(allDeterminedPosDict.Count, determinedPosDict);
            }
        }
        
        for (int i = 0; i < popSiz; i++){
            Dictionary<Vector3, Vector3> determinedPosDict = new Dictionary<Vector3, Vector3>(); // dit is het DNA
            makeDeterminedPosDict(spawnPos, determinedPosDict);
          // makeDeterminedPosDict2(spawnPos);
        }
        
     
        void func(){
            // crossover, voor nu gewoon iets testen: pak gewoon alleen alles van nummer 1 en doe alles veranderen behalve waar de agent niet langs is geweest
            // verander ook het punt voor de dode hoek. 

            // ik moet bhijouden langs welke determinedpositions elke agent is geweest
            // en ik moet niet een fitnessdeterminedposdict hebben
            // of ik moet nog een fitnessAgentDict hebben ofzo waarin key een float is, namelijkd de fitness

            //  makeBestDeterminedPosDict = true;

          //  print("yo what");

           // Dictionary<object,object> bestAgentT = fitnessAgentDict[fitnessList[0]];




            // parameter toevoegen bij makedeterminedposdict of niet? of gewoon if statement om te checken of de size van fitnesslist groter is dan
            // 1 ofzo en dan als dat zo is dan de beste dictionary daar pakken? hmmmm ja laten e dat doen 

            

            allDeterminedPosDict.Clear(); // wacht alldeterminedposdict is gewoon useless

          //  print("1");

            for (int i = 0; i < popSiz; i ++){
                Dictionary<Vector3, Vector3> determinedPosDict = new Dictionary<Vector3, Vector3>(); // dit is het DNA
                makeDeterminedPosDict(spawnPos, determinedPosDict);
            //    yield return new WaitForSeconds(UnityEngine.Random.Range(0f, .1f)); 
            }

            fitnessAgentDict.Clear();
            fitnessDeterminedPosDict.Clear();
            fitnessList.Clear();

            // alle agents destroyeb

        
            foreach (GameObject obj in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (obj.layer == LayerMask.NameToLayer("Agent"))
                {
                    Destroy(obj);
                }
            }

            genCnt++;

            for(int i = 0; i < popSiz; i ++){
                makeAgent(i);
             //   yield return new WaitForSeconds(UnityEngine.Random.Range(0f, .1f)); 
            }    
        }

      
        Dictionary<GameObject, Dictionary<object, object>> agentDict = new Dictionary<GameObject, Dictionary<object,object>>();

        int deadCnt = 0;
       
        Stopwatch stopWatch = Stopwatch.StartNew();

        void makeAgent(int agentNum){
            GameObject agent = GameObject.CreatePrimitive(PrimitiveType.Cube);
            float n = .75f;
            agent.transform.localScale = new Vector3(n,n,n);
            agent.transform.position = spawnPos;
            agent.name = "Agent";
            giveColor(agent, Color.red);
            int layer = LayerMask.NameToLayer("Agent");
            agent.layer = layer;

        


            agentDict.Add(agent, new Dictionary<object, object>());
            Dictionary<object, object> agentT = agentDict[agent];
            agentT.Add("TurnCount", 0);
            agentT.Add("Fitness", 0f);
            Dictionary<Vector3, Vector3> determinedPosDict = allDeterminedPosDict[agentNum];
            agentT.Add("DeterminedPosDict", determinedPosDict);
            List<Vector3> visitedPosList = new List<Vector3>();
            agentT.Add("VisitedPosList", visitedPosList);

            visitedPosList.Add(spawnPos);

            // in een dictinoary als key een position storen waar de agent dan is, dan als value waar de agent naartoe moet tweenen.
            // dus wanneer deze dictionary gemaakt is pas tweenen
            // dode hoek?: returnen -- is al automatisch gedaan omdat er geen paden meer zijn.
            // alle paden al gehad?: returnen
            // einde gehaald?: returnen -- is al automatisch gedaan omdat er geen paden meer zijn: je kan niet terug naar achtern
        
            
            // de positions zijn de genen


            float turnCnt = -1;
            bool dead = false;

            float getFitness(){
                return .03f*(MAG(spawnPos, endPos) / MAG(endPos, agent.transform.position + new Vector3(0, 0.1f, 0))*4f + (turnCnt/10)); // was * turncnt
            }

            void changeColorAndNameOfAgent(){
                float r = 1, g = 0, b = 0;
                    
                float fitness = getFitness();

                agent.name = turnCnt + " Agent " + fitness;

                r = Mathf.Clamp(1 - fitness, 0, 1);
                g = Mathf.Clamp(fitness, 0, 1);

                giveColor(agent, new Color(r, g, b));
            }


            IEnumerator changeColorLoop(){
                while (! dead){
                    changeColorAndNameOfAgent();
                    yield return new WaitForSeconds(.1f);
                }
            }

            StartCoroutine(changeColorLoop());


            // void increaseTurnCnt(){
            //     Debug.Log("INCREAse");
            //     turnCnt++;
            // }


            // population, generation
            // kies welke de fittest zijn
            // dan crossover
            // bij de crossover kans op mutation, bij elke kans op mutation,  
            // repeat


            //crossover: er zijn 

            // ik kan niet zien wat de beste turns zijn, of ja iets anders, maar geen idee hoe ik dit moet zeggen

            // als het eind is gehaald, dan klaar, want dan weten we wat de beste route is. 



            
            void keepTweening(){
                //checks doen

                turnCnt++;

                Vector3 agentPos = agent.transform.position;
                if (agentPos == endPos){
                    endReached = true;
                    stopWatch.Stop();
                    TimeSpan elapsed = stopWatch.Elapsed;
                    double elapsedSeconds = elapsed.TotalSeconds;
                    UnityEngine.Debug.Log($"Tijd: {elapsedSeconds:F2} secondes");
                    UnityEngine.Debug.Log($"Generatie: {genCnt}");
                    UnityEngine.Debug.Log($"Populatie: {popSiz}");
                }

                if (determinedPosDict.ContainsKey(agentPos)){
                    Vector3 finalPos = determinedPosDict[agentPos];
                    visitedPosList.Add(finalPos);

                    Tween tw = agent.transform.DOMove(finalPos, MAG(finalPos, agentPos)/60).SetEase(Ease.Linear);
                    //Tween tw = agent.transform.DOMove(finalPos, 0).SetEase(Ease.Linear);
                    tw.OnComplete(keepTweening);
                }else{
                    dead = true;
                    deadCnt++;

                    agentT["TurnCount"] = turnCnt;
                    agentT["Fitness"] = getFitness();

                    changeColorAndNameOfAgent();

                   // print(deadCnt, popSiz)
               //    print(deadCnt);
               //    print(popSiz);

                  //  print(deadCnt == popSiz);
                 //   print(! endReached);
                
                    if (deadCnt == popSiz &&! endReached){
                        deadCnt = 0;
                        // kies hier de beste
                    //    print("What");
                    //    print("what");


                   //     print("what 2");

                        for (int i = 0; i < agentDict.Count; i++){
                           // print("what i" + i);
                            float highestFitness = 0; // verandert later
                            Dictionary<Vector3,Vector3> bestDeterminedPosDict = new Dictionary<Vector3,Vector3>(); // verandert later
                            GameObject bestAgent = agent; // verandert later
                            Dictionary<object,object> bestAgentT = agentT; // verandert later


                          //  print("reached");
                         //   print(agentDict.Count);
                            // probeer hier hoogste values uit te krijgen 
                            // die determinedposdicts moesten dus vooraf gemaakt zijn, want blokken spawnen niet tegelijk en kunnen eerder of later "dead" zijn.
                            // oh wacht, pas als deadcount gelijk is aan popsize dan doet hij dit, ah blunder. had niet te diep gedacht hiervoor, of zelfs niet eens goed gezien
                            // misschien is het later handig als ik steeds nieuwe generations maak, moeten we dan ff kijken. 

                            foreach((GameObject vAgent, Dictionary<object,object> vAgentT) in agentDict){
                               // print("this");
                                if ((float)vAgentT["Fitness"] > highestFitness){
                                  //  print("this 2");
                                    highestFitness = (float)vAgentT["Fitness"];
                                    bestDeterminedPosDict = (Dictionary<Vector3,Vector3>)vAgentT["DeterminedPosDict"];
                                    bestAgent = vAgent;
                                    bestAgentT = vAgentT;
                                 //   print("loop");
                                }
                            }

                            // print("reached before");

                            // soms zitten er 2 keer dezelfde keys in de dictionary, als dat zo is, dan niet nog een keer adden
                            // dat betekent dat de size van de fitnesslist en de fitnessdeterminedposdict niet altijd gelijk is aan de popsiz
                            agentDict.Remove(bestAgent);

                            if (! fitnessList.Contains(highestFitness)){
                             //   print(highestFitness);
                                fitnessList.Add(highestFitness);
                                fitnessDeterminedPosDict.Add(highestFitness, bestDeterminedPosDict);
                                fitnessAgentDict.Add(highestFitness, bestAgentT);
                               // print("reached after");
                            } else {
                              //  print("DUPLICATE");
                            }
                        }

                       // print("what 3");

                       // foreach(float fitness in fitnessList){
                           // print(fitness);
                      //  }
                    //    print("righyt before");
                     //   StartCoroutine(func());
                        func();
                    }
                }
            }

            keepTweening();



            // kijk welke het dichtstebij zijn bij elke richting, unit hiervoor gebruiken. 


            // gewoon een raycast doen

            // ten eerste doe je een for loop door de tweenposlist
            // je kijkt welke positions dezelfde waarde hebben op de x as, en welke positions dezelfde waarde hebben op de z as
            // dit zijn allemaal kandidaten dus.
            // daarna kijk je of je er niet een muur tussen zit
            // daarna zijn er meerdere opties mogelijk, voor nu kiest de script random tussen die
            // later vooraf kiezen

            //magnitude checks doen

         //   agent.transform.DOMove(new Vector3(0, 100, 0), 10);
        }

      



        for (int i = 0; i < popSiz; i ++){
            makeAgent(i);
          //  yield return new WaitForSeconds(UnityEngine.Random.Range(1f, 2f)); 
           //  yield return new WaitForSeconds(UnityEngine.Random.Range(0f, .1f)); 
        }


        // wat nu? // steeds nieuwe generation als elke agent dood is. niet na x tijd nieuwe agent maken, dat is slecht voor deze ai, want misschien kost het wel heel veel turns
        // niet recursion in de keeptweening loop, dan krijg je stack overflow
        // ergens een while loop die alles update
        // voor nu recursion om gewoon de script te hebben 
        // kan niet zo ver vooruit denken met die while loop in het systeem. 







        // de ai moet beloodnd worden voor hoe ver hij is tot het eind en hoe veel succesvolle turns hij heeft gemaakt
        // de fitness formule = 


        // nu de cubes maken, .75, .75, .75 size








       // nu vinden wat de target positions zijn voor de agents. Dan op die positie een blok spawnen met een bepaalde kleur en dan scalen omlaag, 
       // in een dictionary opslaan wat deze positions zijn. 

       //Hoe vind je de juiste positions? Je kan kijken naar de walls. Je gaat langs elke positie, als hij 2 richtingen op kan, en die richtingen zijn tegengesteld van elkaar,
       // dus ze staan niet loodrecht op elkaar, dan telt deze niet. De rest telt wel. Dus 4 richtingen, 3 richtingen, 2 richtingen loodrecht op elkaar en 1 richting (dode hoek).  

       //Je kijkt dus naar de walls, kijk of de count van de walllist gelijk is aan 0, dat betekent dat er dus geen walls zijn bij een bepaalde direction.
       // Oke maar dit kan later, eerst de maze generation goed maken, hij is al best wel ver.  

        

       //Key en value? 

        


 


        // jammer dat ik niet de juiste dictionary kan accessen met de naam alleen


        // oke nu moet ik het pad maken
        // hoe doe je dat?
        // ten eerste begin je bij de entrance cube, we gaan nu het hoofdpad maken
        // vanaf daar ga je elke richting op bekijken, dus links, voor, rechts, achter
        // als daar een blok is die niet een muur is dan ga je naar dat blok dan telt dat blok als een "pad"
        // je geeft dan een key aan die blok die IsPath heet en een boolean is, namelijk true
        // die cube wordt dan magenta kleur
        // vanaf daar kijk je weer elke kant op, je kijkt of dat blok bij die position niet een pad is en niet een muur, zo ja dan ga je naar dat blok
        // oke dit gaat een tijdje door
        // maar er is wel een regel: er mag niet een grid ontstaan van 2x2 paden zeg maar, ik bedoel gewoon niet een pad van 2x2

        // dus wanneer je vanuit een cube een andere cube magenta te maken, dan moet je vanaf de position van die potentiele magenta cube ook weer 4 richtingen bekijken
        // er mogen maximaal 2 buren zijn: de part waar je eerst van af gaat en de part waar je de cube mee wilt laten connecten
        // als er 3 buren zijn dan al meteen returnen
        // als er 2 buren zijn, dan check je eerst of de nieuwe buur aan de goede kant staat
        // want als je vanaf de eerste buur naar de nieuwe potentiele magenta part gaat heeft dat een richting
        // die richting moet dus gelijk zijn aan de richting waar je vanaf de magenta part kijkt 
        // dan mag er pas een cube gemaakt worden

        // hoe maak je dat het pad altijd uiteindelijk naar de uitgang gaat? 
        // ook gewoon vanuit de exit een pad generaten?
        // dan stoppen wanneer je een pad tegenkomt die niet gemaakt is door de exit?


        // oke maar is dit wel de beste manier om een doolhof te maken?
        // deze code werkt ongeveer als die van the binding of isaac
        // het heeft een probleem denk ik
        // want ik wil wel dat zo veel mogelijk van de map gebruikt wordt
        // om dat te bereiken kan ik nog steeds dezelfde code gebruiken, maar dan meerdere vaste punten pakken in de maap
        // vanaf waar de script een pad maakt
        // dus linksonder, onder, rehchtsonder zijn punten, maar onder is al een punt voor het hoofdpad
        // vanaf daar dan gewoon generaten random kanten op


        // oke dan heb je het hoofdpad, dan zijpaden maken, kan later


        // oke nee dit moet anders


        // Random rng = new Random();
        // void Shuffle<T>(T[] array) {
        //     int n = array.Length;
        //     while (n > 1)
        //     {
        //         n--;
        //         int k = rng.Next(n + 1);
        //         T value = array[k];
        //         array[k] = array[n];
        //         array[n] = value;
        //     }
        // }



        // while (true){
        //     GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        //     cube.transform.position = new Vector3(0, 10, 0) + new Vector3(0,cnt,0);
        //     cube.transform.rotation = Quaternion.Euler(-30*cnt,30*cnt,0);
        //     cube.name = "berend" + "_" + cnt;
        //     cnt++;

        //     yield return new WaitForSeconds(.1f);
        // }

        // for (int i = 0; i < 5; i++){
        //     GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        //     cube.transform.position = new Vector3(0, 10, 0)*i;
        //     cube.transform.rotation = Quaternion.Euler(-30*i,30*i,0);
        //     cube.name = "berend";
        // }
 

    }

    // Update is called once per frame
    void Update()
    {
        // if(! endReached) {
        //     totalTime += Time.deltaTime;
        //     totalTimeText.text = "Total Time: " + ((float)Math.Round(totalTime, 1)).ToString();
        // }
        // float curVal = float.Parse(totalTimeText.text);
        // float newVal = curVal + Time.deltaTime;
        // totalTimeText.text = newVal.ToString();
    }
}
