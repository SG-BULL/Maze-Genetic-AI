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


public class MazeAndGeneticAI : MonoBehaviour
{
    bool endReached = false;
    System.Random random = new System.Random();

    void Awake(){
        UnityEngine.Random.InitState(100); 
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

        int gridSize = 30;
        Vector3 spawnPos = Vector3.zero; 
        Vector3 endPos = Vector3.zero;
        
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
            return (toThis-fromThis).normalized;
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

            if (neighCnt > 0){ 
                int index = getRandomNumber(0f, (float)neighCnt-1);

                Vector3 finalDir = dirList[index]; 
                Vector3 nextPos = pos + finalDir; 
                List<GameObject> wallList = wallDict[pos + finalDir *.5f]; 

                dirHistoryList.Add(finalDir);

                foreach(GameObject wall in wallList) { 
                    Destroy(wall); 
                }

                wallList.RemoveAll(item => item == null);
                wallList.RemoveAll(item => item is GameObject);
                
                gridDict[nextPos].Add("FromNeighbourPos", pos); 
                generatePathOnPos(nextPos); 

            }else{ 
                int lastIndex = dirHistoryList.Count - 1;
                if (lastIndex < 0) return;

                Vector3 prevDir = dirHistoryList[lastIndex];
                dirHistoryList.RemoveAt(lastIndex);

                NEXTPOS = pos - prevDir;
                return;
            }
        } 

        foreach((Vector3 pos, Dictionary<object, object> dict) in gridDict){ 
            if (dict.ContainsKey("IsEntrance")) { 
                generatePathOnPos(pos); 
                break; 
            } 
        } 

        int LASTINDEX = dirHistoryList.Count - 1;

        while (LASTINDEX >= 0){
            LASTINDEX = dirHistoryList.Count - 1;
            generatePathOnPos(NEXTPOS);
        };

        yield return new WaitForSeconds(1f);

        foreach((Vector3 pos, Dictionary<object, object> dict) in gridDict) { 
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

        List<Vector3> tweenPosList = new List<Vector3>();

        foreach((Vector3 pos, Dictionary<object,object> cubeDict) in gridDict){
            if (! cubeDict.ContainsKey("IsSpawn") ||! cubeDict.ContainsKey("IsEnd")) {
                List<Vector3> dirList = new List<Vector3>();

                foreach(Vector3 dir in dirArr) {
                    if (gridDict.ContainsKey(pos + dir)){
                        List<GameObject> wallList = wallDict[pos + dir*.5f];
        
                        if (wallList.Count == 0){
                            dirList.Add(dir);
                        }
                    }
                }

                if (dirList.Count == 2){
                    Vector3 dir1 = dirList[0];
                    Vector3 dir2 = dirList[1];

                    if (dir1 + dir2 != new Vector3(0,0,0)){
                        tweenPosList.Add(pos);
                    }

                }else{
                    tweenPosList.Add(pos);
                }   
            }
        }

        yield return new WaitForSeconds(.5f);
        Dictionary<int, Dictionary<Vector3, Vector3>> allDeterminedPosDict = new Dictionary<int, Dictionary<Vector3, Vector3>>();

        LayerMask layerMask = LayerMask.GetMask("Agent");

        int popSiz = 50;
        int genCnt = 1;
      
        List<float> fitnessList = new List<float>();
        Dictionary<float, Dictionary<Vector3,Vector3>> fitnessDeterminedPosDict = new Dictionary<float, Dictionary<Vector3,Vector3>>();
        Dictionary<float, Dictionary<object,object>> fitnessAgentDict = new Dictionary<float, Dictionary<object,object>>();
        
        void makeDeterminedPosDict(Vector3 agentPos, Dictionary<Vector3, Vector3> determinedPosDict){
             List<Vector3> makeFinalPosList(Vector3 vAgentPos){
                List<Vector3> potentialPosList = new List<Vector3>();

                foreach(Vector3 pos in tweenPosList){
                    if ((pos.x == vAgentPos.x || pos.z == vAgentPos.z) &&  pos != vAgentPos){
                        potentialPosList.Add(pos);
                    } 
                }

                List<Vector3> removeList = new List<Vector3>();

                foreach(Vector3 pos in potentialPosList){
                    if (Physics.Raycast(vAgentPos, UNIT(pos, vAgentPos), out RaycastHit hit, MAG(pos, vAgentPos), ~layerMask)){
                        removeList.Add(pos);
                    }
                }

                foreach(Vector3 pos in removeList){
                    potentialPosList.Remove(pos);
                }

                Dictionary<Vector3, object> closestPosDict = new Dictionary<Vector3, object>();

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
           
            if (fitnessList.Count > 0 && determinedPosDict.Count == 0) { 
                Dictionary<object,object> bestAgentT = fitnessAgentDict[fitnessList[getRandomNumber(0f, Mathf.Clamp(2f, 1f, (float)fitnessList.Count-1))]]; 

                List<Vector3> bestVisitedPosList = (List<Vector3>)bestAgentT["VisitedPosList"];
                List<Vector3> targetPosList = new List<Vector3>();
               
                for (int i = bestVisitedPosList.Count - 1; i >= 0; i--){
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

                Vector3 targetPos = new Vector3(.66f,66f,.66f);

                for(int i = 0; i < targetPosList.Count; i++){
                    if (getRandomNumber(1f, 100f) <= 65 || i == targetPosList.Count - 1){
                        targetPos = targetPosList[i];
                        break;
                    }
                }

                for (int i = bestVisitedPosList.Count - 1; i >= 0; i--){ 
                    Vector3 pos = bestVisitedPosList[i];
                    
                    if (pos == targetPos){
                        break;
                    } else {
                        bestVisitedPosList.Remove(pos);
                    }
                }

                for (int i = 0; i < bestVisitedPosList.Count; i++){
                    Vector3 pos = bestVisitedPosList[i];
                    
                    if (i < bestVisitedPosList.Count-1){
                        determinedPosDict.Add(pos, bestVisitedPosList[i + 1]); 
                    }
                }

                makeDeterminedPosDict(targetPos, determinedPosDict);

                return;
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
            Dictionary<Vector3, Vector3> determinedPosDict = new Dictionary<Vector3, Vector3>();
            makeDeterminedPosDict(spawnPos, determinedPosDict);
        }
        
     
        void func(){
            allDeterminedPosDict.Clear(); 

            for (int i = 0; i < popSiz; i ++){
                Dictionary<Vector3, Vector3> determinedPosDict = new Dictionary<Vector3, Vector3>(); 
                makeDeterminedPosDict(spawnPos, determinedPosDict);
            }

            fitnessAgentDict.Clear();
            fitnessDeterminedPosDict.Clear();
            fitnessList.Clear();

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

            float turnCnt = -1;
            bool dead = false;

            float getFitness(){
                return .03f*(MAG(spawnPos, endPos) / MAG(endPos, agent.transform.position + new Vector3(0, 0.1f, 0))*4f + (turnCnt/10)); 
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
            
            void keepTweening(){
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
                    tw.OnComplete(keepTweening);
                }else{
                    dead = true;
                    deadCnt++;

                    agentT["TurnCount"] = turnCnt;
                    agentT["Fitness"] = getFitness();

                    changeColorAndNameOfAgent();

                    if (deadCnt == popSiz &&! endReached){
                        deadCnt = 0;

                        for (int i = 0; i < agentDict.Count; i++){
                            float highestFitness = 0; 
                            Dictionary<Vector3,Vector3> bestDeterminedPosDict = new Dictionary<Vector3,Vector3>(); 
                            GameObject bestAgent = agent; 
                            Dictionary<object,object> bestAgentT = agentT; 

                            foreach((GameObject vAgent, Dictionary<object,object> vAgentT) in agentDict){
                                if ((float)vAgentT["Fitness"] > highestFitness){
                                    highestFitness = (float)vAgentT["Fitness"];
                                    bestDeterminedPosDict = (Dictionary<Vector3,Vector3>)vAgentT["DeterminedPosDict"];
                                    bestAgent = vAgent;
                                    bestAgentT = vAgentT;
                                }
                            }

                            agentDict.Remove(bestAgent);

                            if (! fitnessList.Contains(highestFitness)){
                                fitnessList.Add(highestFitness);
                                fitnessDeterminedPosDict.Add(highestFitness, bestDeterminedPosDict);
                                fitnessAgentDict.Add(highestFitness, bestAgentT);
                            } else {
                            }
                        }

                        func();
                    }
                }
            }

            keepTweening();
        }

        for (int i = 0; i < popSiz; i ++){
            makeAgent(i);
        }
    }
    void Update()
    {

    }
}
