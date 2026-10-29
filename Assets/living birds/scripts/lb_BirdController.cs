using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class lb_BirdController : MonoBehaviour {
	public int idealNumberOfBirds;
	public int maximumNumberOfBirds;
	public Camera currentCamera;
	public float unspawnDistance = 10.0f;
	public bool highQuality = true;
	public bool collideWithObjects = true;
	public LayerMask groundLayer;
	public float birdScale = 1.0f;

	public bool robin = true;
	public bool blueJay = true;
	public bool cardinal = true;
	public bool chickadee = true;
	public bool sparrow = true;
	public bool goldFinch = true;
	public bool crow = true;

	bool pause = false;
	GameObject[] myBirds;
	List<string> myBirdTypes = new List<string>();
	List<GameObject> birdGroundTargets = new List<GameObject>();
	List<GameObject> birdPerchTargets = new List<GameObject>();
	int activeBirds = 0;
	int birdIndex = 0;
	GameObject[] featherEmitters = new GameObject[3];

	public void AllFlee(){
		if(!pause){
			for(int i=0;i<myBirds.Length;i++){
				if(myBirds[i].activeSelf){
					myBirds[i].SendMessage ("Flee");
				}
			}
		}
	}
	
	public void Pause(){
		if(pause){
			AllUnPause ();
		}else{
			AllPause ();
		}
	}
	
	public void AllPause(){
		pause = true;
		for(int i=0;i<myBirds.Length;i++){
			if(myBirds[i].activeSelf){
				myBirds[i].SendMessage ("PauseBird");
			}
		}
	}
	
	public void AllUnPause(){
		pause = false;
		for(int i=0;i<myBirds.Length;i++){
			if(myBirds[i].activeSelf){
				myBirds[i].SendMessage ("UnPauseBird");
			}
		}
	}

	public void SpawnAmount(int amt){
		for(int i=0;i<=amt;i++){
			SpawnBird ();
		}
	}

	public void ChangeCamera(Camera cam){
		currentCamera = cam;
	}

	void Start () {
		if (currentCamera == null){
			GameObject mainCamObj = GameObject.FindGameObjectWithTag("MainCamera");
			if(mainCamObj != null) {
				currentCamera = mainCamObj.GetComponent<Camera>();
			}
		}

		if(idealNumberOfBirds >= maximumNumberOfBirds){
			idealNumberOfBirds = maximumNumberOfBirds-1;
		}

		if(robin) myBirdTypes.Add ("lb_robin");
		if(blueJay) myBirdTypes.Add ("lb_blueJay");
		if(cardinal) myBirdTypes.Add ("lb_cardinal");
		if(chickadee) myBirdTypes.Add ("lb_chickadee");
		if(sparrow) myBirdTypes.Add ("lb_sparrow");
		if(goldFinch) myBirdTypes.Add ("lb_goldFinch");
		if(crow) myBirdTypes.Add ("lb_crow");

		myBirds = new GameObject[maximumNumberOfBirds];
		GameObject bird;
		for(int i=0;i<myBirds.Length;i++){
			if(highQuality){
				bird = Resources.Load (myBirdTypes[Random.Range (0,myBirdTypes.Count)]+"HQ",typeof(GameObject)) as GameObject;
			}else{
				bird = Resources.Load (myBirdTypes[Random.Range (0,myBirdTypes.Count)],typeof(GameObject)) as GameObject;
			}
			myBirds[i] = Instantiate (bird,Vector3.zero,Quaternion.identity) as GameObject;
			myBirds[i].transform.localScale = myBirds[i].transform.localScale*birdScale;
			myBirds[i].transform.parent = transform;
			myBirds[i].SendMessage ("SetController",this);
			myBirds[i].SetActive (false);
		}

		GameObject[] groundTargets = GameObject.FindGameObjectsWithTag("lb_groundTarget");
		GameObject[] perchTargets = GameObject.FindGameObjectsWithTag("lb_perchTarget");

		if(currentCamera != null) {
			for (int i=0;i<groundTargets.Length;i++){
				if(Vector3.Distance (groundTargets[i].transform.position,currentCamera.transform.position)<unspawnDistance){
					birdGroundTargets.Add(groundTargets[i]);
				}
			}
			for (int i=0;i<perchTargets.Length;i++){
				if(Vector3.Distance (perchTargets[i].transform.position,currentCamera.transform.position)<unspawnDistance){
					birdPerchTargets.Add(perchTargets[i]);
				}
			}
		}

		GameObject fEmitter = Resources.Load ("featherEmitter",typeof(GameObject)) as GameObject;
		for(int i=0;i<3;i++){
			featherEmitters[i] = Instantiate (fEmitter,Vector3.zero,Quaternion.identity) as GameObject;
			featherEmitters[i].transform.parent = transform;
			featherEmitters[i].SetActive (false);
		}
	}

	void OnEnable(){
		InvokeRepeating("UpdateBirds",1,1);
		StartCoroutine("UpdateTargets");
	}

	Vector3 FindPointInGroundTarget(GameObject target){
		Collider col = target.GetComponent<Collider>();
		if(col == null) return target.transform.position;

		Vector3 point;
		point.x = Random.Range (col.bounds.max.x,col.bounds.min.x);
		point.y = col.bounds.max.y;
		point.z = Random.Range (col.bounds.max.z,col.bounds.min.z);

		RaycastHit hit;
		if (Physics.Raycast (point,-Vector3.up,out hit,col.bounds.size.y,groundLayer)){
			return hit.point;
		}

		return point;
	}

	void UpdateBirds(){
		if(currentCamera == null) return;

		if(activeBirds < idealNumberOfBirds  && AreThereActiveTargets()){
			SpawnBird();
		}else if(activeBirds < maximumNumberOfBirds && Random.value < .05 && AreThereActiveTargets()){
			SpawnBird();
		}

		if(myBirds != null && myBirds.Length > 0 && myBirds[birdIndex].activeSelf && BirdOffCamera (myBirds[birdIndex].transform.position) && Vector3.Distance(myBirds[birdIndex].transform.position,currentCamera.transform.position) > unspawnDistance){
			Unspawn(myBirds[birdIndex]);
		}

		if(myBirds != null && myBirds.Length > 0) {
			birdIndex = birdIndex == myBirds.Length-1 ? 0:birdIndex+1;
		}
	}

	IEnumerator UpdateTargets(){
		List<GameObject> gtRemove = new List<GameObject>();
		List<GameObject> ptRemove = new List<GameObject>();

		while(true){
			if(currentCamera != null) {
				gtRemove.Clear();
				ptRemove.Clear();

				for(int i=0;i<birdGroundTargets.Count;i++){
					if (Vector3.Distance (birdGroundTargets[i].transform.position,currentCamera.transform.position)>unspawnDistance){
						gtRemove.Add (birdGroundTargets[i]);
					}
					yield return 0;
				}
				for (int i=0;i<birdPerchTargets.Count;i++){
					if (Vector3.Distance (birdPerchTargets[i].transform.position,currentCamera.transform.position)>unspawnDistance){
						ptRemove.Add (birdPerchTargets[i]);
					}
					yield return 0;
				}

				foreach (GameObject entry in gtRemove){
					birdGroundTargets.Remove(entry);
				}
				foreach (GameObject entry in ptRemove){
					birdPerchTargets.Remove(entry);
				}
				yield return 0;

				Collider[] hits = Physics.OverlapSphere(currentCamera.transform.position,unspawnDistance);
				foreach(Collider hit in hits){
					if (hit.tag == "lb_groundTarget" && !birdGroundTargets.Contains (hit.gameObject)){
						birdGroundTargets.Add (hit.gameObject);
					}
					if (hit.tag == "lb_perchTarget" && !birdPerchTargets.Contains (hit.gameObject)){
						birdPerchTargets.Add (hit.gameObject);
					}
				}
			}
			yield return 0;
		}
	}

	bool BirdOffCamera(Vector3 birdPos){
		if(currentCamera == null) return false;
		Vector3 screenPos = currentCamera.WorldToViewportPoint(birdPos);
		if (screenPos.x < 0 || screenPos.x > 1 || screenPos.y < 0 || screenPos.y > 1){
			return true;
		}else{
			return false;
		}
	}

	void Unspawn(GameObject bird){
		bird.transform.position = Vector3.zero;
		bird.SetActive (false);
		activeBirds --;
	}

	void SpawnBird(){
		if (!pause){
			GameObject bird = null;
			int randomBirdIndex = Mathf.FloorToInt (Random.Range (0,myBirds.Length));
			int loopCheck = 0;

			while(bird == null){
				if(myBirds[randomBirdIndex].activeSelf == false){
					bird = myBirds[randomBirdIndex];
				}
				randomBirdIndex = randomBirdIndex+1 >= myBirds.Length ? 0:randomBirdIndex+1;
				loopCheck ++;
				if (loopCheck >= myBirds.Length){
					return;
				}
			}

			bird.transform.position = FindPositionOffCamera();
			if(bird.transform.position == Vector3.zero){
				return;
			}else{
				bird.SetActive (true);
				activeBirds++;
				BirdFindTarget(bird);
			}
		}
	}

	bool AreThereActiveTargets(){
		return (birdGroundTargets.Count > 0 || birdPerchTargets.Count > 0);
	}

	Vector3 FindPositionOffCamera(){
		if(currentCamera == null) return Vector3.zero;

		RaycastHit hit;
		float dist = Random.Range (2,10);
		Vector3 ray = -currentCamera.transform.forward;
		int loopCheck = 0;

		ray += new Vector3(Random.Range (-.5f,.5f),Random.Range (-.5f,.5f),Random.Range (-.5f,.5f));

		while(Physics.Raycast(currentCamera.transform.position,ray,out hit,dist)){
			dist = Random.Range (2,10);
			loopCheck++;
			if (loopCheck > 35){
				return Vector3.zero;
			}
		}
		return currentCamera.transform.position+(ray*dist);
	}
	
	void BirdFindTarget(GameObject bird){
		GameObject target;
		if (birdGroundTargets.Count > 0 || birdPerchTargets.Count > 0){
			float gtArea=0.0f;
			float ptArea=birdPerchTargets.Count*0.3f;

			for (int i=0;i<birdGroundTargets.Count;i++){
				Collider col = birdGroundTargets[i].GetComponent<Collider>();
				if(col != null) {
					gtArea += col.bounds.size.x*col.bounds.size.z;
				}
			}
			if (ptArea == 0.0f || Random.value < gtArea/(gtArea+ptArea)){
				target = birdGroundTargets[Mathf.FloorToInt (Random.Range (0,birdGroundTargets.Count))];
				bird.SendMessage ("FlyToTarget",FindPointInGroundTarget(target));
			}else{
				target = birdPerchTargets[Mathf.FloorToInt (Random.Range (0,birdPerchTargets.Count))];
				bird.SendMessage ("FlyToTarget",target.transform.position);
			}
		}else{
			Vector3 camPos = currentCamera != null ? currentCamera.transform.position : Vector3.zero;
			bird.SendMessage ("FlyToTarget",camPos+new Vector3(Random.Range (-100,100),Random.Range (5,10),Random.Range(-100,100)));
		}
	}

	void FeatherEmit(Vector3 pos){
		foreach (GameObject fEmit in featherEmitters){
			if(!fEmit.activeSelf){
				fEmit.transform.position = pos;
				fEmit.SetActive (true);
				StartCoroutine("DeactivateFeathers",fEmit);
				break;
			}
		}
	}

	IEnumerator DeactivateFeathers(GameObject featherEmit){
		yield return new WaitForSeconds(4.5f);
		featherEmit.SetActive (false);
	}
}