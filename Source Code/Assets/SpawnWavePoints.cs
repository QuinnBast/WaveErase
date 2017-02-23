using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SpawnWavePoints : MonoBehaviour {

public GameObject spherePrefab;
public GameObject mainCamera;
public GameObject scoreText;
public GameObject guide1;
public GameObject guide2;
private GameObject lines;


/* Canvas in the game for UI and menu navigation */
public GameObject mainMenuPage;
public GameObject highscorePage;
public GameObject gameUI;
public GameObject newHighscorePage;
public GameObject optionsPage;
public Text highscoreName;

//public const int numWavePoints = 500;

private Difficulties difficulty = Difficulties.Tranquil;

public int numWavePoints; // if we are doing multiple speeds for game modes we have to account for decay. 
public float decayFactor;
private bool isPaused;
private int dangerPoint;
private int dangerWidth;
private int dangerLeft;
private int dangerRight;
private const float scalar = 10f;
public Shader lineShader;


enum TutorialStates { inMenu, 
					  waitFirstWave, 
					  moveWFirstWave, 
					  cancelFirstWave, 
					  moveWSecondWave, 
					  AddFirstWaveY, 
					  AddFirstWaveR, 
					  WaitSecondWave }
enum Difficulties {Tranquil, Lively, Chaotic};
enum GameStates {Menu, Play, Options, HighScore, Quit, LevelEnd, NewHighscore};
public enum dangerState {Fine, Okay, Bad};
public enum waveTypes {HalfSine, FullSine, Square, Triangle, SawTooth};

private int score;
private GameStates gameState;
private dangerState previousDangerState = dangerState.Fine;
private dangerState currentDangerState = dangerState.Fine;
private int dangerCountdown;
private int levelFrame = 0;
private Wave currentWave;

private int redFrames = 0;

private float [] buffer;
private float [] shiftRightHeights;
private float [] shiftLeftHeights;

public GameObject [] tutorialPanels;
private TutorialStates currentTutorialState = TutorialStates.inMenu;
private bool inTutorial = false;
private int tutorialFrame = 0;

private Level currentLevel;

	public class Wave {
		public waveTypes waveType;
		public float amplitude;
		public int width;
		public int startTime;
		
		public Wave (waveTypes wavetype, float amplitude, int width, int startTime) {
			this.waveType = wavetype;
			this.amplitude = amplitude;
			this.width = width;
			this.startTime = startTime;
			
		}
	}

	public class Level {
		public int length;
		public Wave [] waves;
		public int currentWaveAdding;
		public int currentWaveReading;
		
		public Level (int length) {
			this.length = length;
			waves = new Wave [length];
			currentWaveAdding = 0;
			currentWaveReading = 0;
		}
		
		public void addWave (waveTypes wavetype, float amplitude, int width, int startTime) {
			waves[currentWaveAdding] = new Wave (wavetype, amplitude, width, startTime);
			currentWaveAdding++;
		}
		
		public Wave getNextWave () {
			currentWaveReading++;
			if (currentWaveReading > length) {
				return null;
			}
			return waves[currentWaveReading-1];
		}
	}

	// Use this for initialization
	void Start () { 
		isPaused = false;
		lines = new GameObject();
        lines.transform.position = new Vector3(0,0,0);
        lines.AddComponent<LineRenderer>();
        LineRenderer lr = lines.GetComponent<LineRenderer>();
		updateDifficultySettings();
		highscorePage.SetActive(false);
		newHighscorePage.SetActive(false);
		gameUI.SetActive(false);
		mainMenuPage.SetActive(true);
		optionsPage.SetActive(false);
		

        //lr.material = new Material(Shader.Find("Particles/Alpha Blended Premultiply"));
		//lr.material = new Material(Shader.Find("Particles/Additive"));
		lr.material = new Material(lineShader);
        lr.startColor = new Color(1,0,0);
        lr.endColor = new Color(0,0,1);
        lr.startWidth = 0.01f;
        lr.endWidth = 0.01f;
		
		// createMyLevel
		//makeTriangleWave (100, 0.5f);
		//makeSquareWave (100, 0.30f);
		addLevel1();
		//makeSineWave (100, 0.3f);
		//makeTriangleWave (100, 0.5f);
		//makeSquareWave (100, -0.30f);
		//makeHeartbeat(100, 0.3f);
		//makeFullSineWave(100, 0.3f);
		//makeSawtoothWave(100, 0.3f);
		//makeArcedWave(100, 0.3f);
	}
	
	// Update is called once per frame
	void FixedUpdate () {
	if (Input.GetKeyDown("space")){
		togglePause();
	}
	
	if (isPaused){
		
	} else if (inTutorial) {
		warnOrKill();
		updateLines();
		if (currentTutorialState == TutorialStates.inMenu) {
		shiftWaves();
		} else if (currentTutorialState == TutorialStates.waitFirstWave) {
			shiftWaves();		
			if (shiftLeftHeights[numWavePoints/2] != 0) {
				showTutorialPanel05();
			}
		} else if (currentTutorialState == TutorialStates.moveWFirstWave) {
			
			shiftWavesIfWinning();
			if (shiftLeftHeights[numWavePoints * 5/8] == 0) {
				showTutorialPanel06();
			}
		} else if (currentTutorialState == TutorialStates.cancelFirstWave) {
			shiftWavesIfWinning();
			if (shiftLeftHeights[numWavePoints/2] == 0) {
				showTutorialPanel07();
			}
		}else if (currentTutorialState == TutorialStates.WaitSecondWave) {
			shiftWaves();		
			if (shiftLeftHeights[numWavePoints/2] != 0) {
				showTutorialPanel09();
			}
		} else if (currentTutorialState == TutorialStates.moveWSecondWave) {			
			shiftWavesIfDying();
			if (currentDangerState == dangerState.Okay) {
				showTutorialPanel10();
			}
		} else if (currentTutorialState == TutorialStates.AddFirstWaveY) {
			shiftWavesIfDying();
			if (currentDangerState == dangerState.Bad) {
				showTutorialPanel11();
			}
		} else if (currentTutorialState == TutorialStates.AddFirstWaveR) {
			shiftWavesIfDying();
			if (currentDangerState == dangerState.Okay) {
				showTutorialPanel12();
			}
		}
		
		
	} else {

		//for (int i = 0; i < numWavePoints; i++) { 
		//	heights[i] = 0.01f;
		//		waves[i].GetComponent<Transform>().localPosition = new Vector3(((i * 1.6f / (float) numWavePoints) - 0.8f), 0.25f, 0);
		//}
		shiftWaves();
		for (int i = 0; i < numWavePoints; i++) { 
			float height = shiftRightHeights[i] + shiftLeftHeights[i];
			if (height > 0.5f) { height = 0.5f; } 
			if (height < -0.5f) { height = -0.5f; }
		}
		warnOrKill();
		punishAverage();
		//cancelDangerZone();
		if (gameState == GameStates.Play){
			addScore();
		}
		updateLines();
		//if (Random.value < 0.01){
		//createRandomWave(0.25f);
		//}
		generateLevelWaves();
		levelFrame++;
		
		if (levelFrame >= 4000 && gameState == GameStates.Play){ // DON'T HARDCODE 4000. Length of wave + 100 (Varible by speed) + last frame the wave appears
			gameState = GameStates.LevelEnd;
			checkHighscore();
			levelFrame = 0;
			currentLevel.currentWaveReading = 0;
			currentWave = null;
		} else if ( levelFrame >= 4000 ){
			levelFrame = 0;
			currentLevel.currentWaveReading = 0;
			currentWave = null;
		}
	}
		
	}
	
	public void generateLevelWaves() {
		if (currentWave == null) {
			currentWave = currentLevel.getNextWave();
		}
		if (currentWave != null) {
			if ( levelFrame == currentWave.startTime) {
				switch (currentWave.waveType) {
					case waveTypes.HalfSine:
						makeSineWave (currentWave.width, currentWave.amplitude);
						break;
					case waveTypes.FullSine:
						
						break;
					case waveTypes.Square:
						makeSquareWave (currentWave.width, currentWave.amplitude);
						break;
					case waveTypes.Triangle:
						
						makeTriangleWave(currentWave.width, currentWave.amplitude);
						break;
					case waveTypes.SawTooth:
						
						break;
				}
				currentWave = null;
			}
		}
	}
	
	 public void updateLines() {
        LineRenderer lr = lines.GetComponent<LineRenderer>();
        Vector3 [] positions = new Vector3 [numWavePoints-1];
        for (int i = 0; i < numWavePoints-1; i++) {
            positions[i] = new Vector3(((i * 1.6f / (float) numWavePoints) - 0.8f), shiftLeftHeights[i] + shiftRightHeights[i], 0);
        }
        lr.SetPositions(positions);
    }
	
	void addLevel1() {
		currentLevel = new Level(19);
		currentLevel.addWave(waveTypes.HalfSine, 0.25f, 100, 100);
		currentLevel.addWave(waveTypes.HalfSine, 0.25f, 100, 400);
		currentLevel.addWave(waveTypes.Square, -0.25f, 100, 700);
		currentLevel.addWave(waveTypes.HalfSine, 0.25f, 100, 1000);
		currentLevel.addWave(waveTypes.HalfSine, -0.25f, 100, 1100);		
		currentLevel.addWave(waveTypes.HalfSine, 0.40f, 100, 1400);
		currentLevel.addWave(waveTypes.Square, -0.25f, 100, 1700);
		currentLevel.addWave(waveTypes.Square, 0.25f, 100, 1800);
		currentLevel.addWave(waveTypes.HalfSine, 0.25f, 100, 2100);
		currentLevel.addWave(waveTypes.HalfSine, -0.25f, 100, 2200);
		currentLevel.addWave(waveTypes.HalfSine, 0.25f, 100, 2500);
		currentLevel.addWave(waveTypes.HalfSine, -0.25f, 100, 2600);
		currentLevel.addWave(waveTypes.HalfSine, 0.25f, 100, 2700);
		currentLevel.addWave(waveTypes.HalfSine, -0.25f, 100, 2800);
		currentLevel.addWave(waveTypes.HalfSine, 0.45f, 100, 3000);
		currentLevel.addWave(waveTypes.HalfSine, -0.2f, 100, 3100);
		currentLevel.addWave(waveTypes.HalfSine, 0.45f, 80, 3200);
		currentLevel.addWave(waveTypes.HalfSine, -0.2f, 80, 3280);
		currentLevel.addWave(waveTypes.Square, 0.2f, 80, 3360);
		
	}
	
	/*public void setHeightOfPoint (int wavePoint, float amplitude) {
		if (wavePoint < 0 || wavePoint > numWavePoints - 1) {
			return;
		}
		waves[wavePoint].GetComponent<Transform>().localPosition = new Vector3(((wavePoint * 1.6f / (float) numWavePoints) - 0.8f), amplitude, 0);
	}*/
	
	
	public void shiftWavesIfDying () {
		if (isDying()) {			
			// each of the shift left thingies must shift left
			for (int i = 0; i < numWavePoints-1; i++) {
				shiftLeftHeights[i] = shiftLeftHeights[i+1];
			}
			// and number 99 should be the leftmost from the buffer.
				//shiftLeftHeights[numWavePoints-1] = buffer [0] + shiftRightHeights[numWavePoints-1]; Bounce Back Game Mechanic
				shiftLeftHeights[numWavePoints-1] = buffer [0];
			// each thing in the buffer should shift left
			for (int i = 0; i < numWavePoints-1; i++) {
				buffer[i] = buffer[i+1];
			}
			// and the rightmost thing should be set to zero
				buffer[numWavePoints-1] = 0;
			// each of the shift right thingies must shift right`
			for (int i = numWavePoints - 1; i >= 1; i--) {
				shiftRightHeights[i] = shiftRightHeights[i-1];
			}
			// and the leftmost should be the mouse position
		}
		shiftRightHeights[0] = Input.mousePosition.y / Screen.height - 0.5f; // will change when we get mouse input working.
		
		//Make the player's outgoing waves smaller past the danger zone
		//attenuatePlayerWaves();
		
		        // show the guides
        moveGuides (shiftLeftHeights[numWavePoints/2]);
		
		attenuatePlayerWaves();
		attenuateEnemyWaves();
	}
	
	public void shiftWavesIfWinning () {
		if (isFollowing()) {
			
			// each of the shift left thingies must shift left
			for (int i = 0; i < numWavePoints-1; i++) {
				shiftLeftHeights[i] = shiftLeftHeights[i+1];
			}
			// and number 99 should be the leftmost from the buffer.
				//shiftLeftHeights[numWavePoints-1] = buffer [0] + shiftRightHeights[numWavePoints-1]; Bounce Back Game Mechanic
				shiftLeftHeights[numWavePoints-1] = buffer [0];
			// each thing in the buffer should shift left
			for (int i = 0; i < numWavePoints-1; i++) {
				buffer[i] = buffer[i+1];
			}
			// and the rightmost thing should be set to zero
				buffer[numWavePoints-1] = 0;
			// each of the shift right thingies must shift right`
			for (int i = numWavePoints - 1; i >= 1; i--) {
				shiftRightHeights[i] = shiftRightHeights[i-1];
			}
			// and the leftmost should be the mouse position
		}
		shiftRightHeights[0] = Input.mousePosition.y / Screen.height - 0.5f; // will change when we get mouse input working.
		
		//Make the player's outgoing waves smaller past the danger zone
		//attenuatePlayerWaves();
		
		        // show the guides
        moveGuides (shiftLeftHeights[numWavePoints/2]);
		
		attenuatePlayerWaves();
		attenuateEnemyWaves();
	}
	
	public void shiftWaves () {
		// each of the shift left thingies must shift left
		for (int i = 0; i < numWavePoints-1; i++) {
			shiftLeftHeights[i] = shiftLeftHeights[i+1];
		}
		// and number 99 should be the leftmost from the buffer.
			//shiftLeftHeights[numWavePoints-1] = buffer [0] + shiftRightHeights[numWavePoints-1]; Bounce Back Game Mechanic
			shiftLeftHeights[numWavePoints-1] = buffer [0];
		// each thing in the buffer should shift left
		for (int i = 0; i < numWavePoints-1; i++) {
			buffer[i] = buffer[i+1];
		}
		// and the rightmost thing should be set to zero
			buffer[numWavePoints-1] = 0;
		// each of the shift right thingies must shift right`
		for (int i = numWavePoints - 1; i >= 1; i--) {
			shiftRightHeights[i] = shiftRightHeights[i-1];
		}
		// and the leftmost should be the mouse position
		shiftRightHeights[0] = Input.mousePosition.y / Screen.height - 0.5f; // will change when we get mouse input working.
		
		//Make the player's outgoing waves smaller past the danger zone
		//attenuatePlayerWaves();
		
		        // show the guides
        moveGuides (shiftLeftHeights[numWavePoints/2]);
		
		attenuatePlayerWaves();
		attenuateEnemyWaves();
	}
	
	public void attenuatePlayerWaves () {
		for (int i = numWavePoints/4; i < numWavePoints/2; i++) {
			shiftRightHeights[i] *= decayFactor;
		}
	}
	
	public void attenuateEnemyWaves () {
		for (int i = 0; i < numWavePoints/4; i++) {
			shiftLeftHeights[i] *= decayFactor;
		}
	}
	
    public void moveGuides (float amplitude) {
        guide1.GetComponent<Transform>().localPosition = new Vector3(0, amplitude, 1);
        guide2.GetComponent<Transform>().localPosition = new Vector3(0, -amplitude, 1);
    }
	
	
	public void makeSineWave(int width, float amplitude) {
	float period = ( Mathf.PI / width);
		for (int i=0; i<width; i++) {
			buffer[i] += Mathf.Sin(period * i) * amplitude;
		}
	}

	public void makeSquareWave(int width, float amplitude){
		for (int i=0; i<width; i++) {
			buffer[i] += amplitude;
		}
	}

	public void makeTriangleWave(int width, float amplitude){
		int i = 0;
		while (i < (width/2)) {
			//set the height of the buffer upwards as a ratio of the maximum
			buffer[i] += amplitude * (i / ((float)width/2));
			i++;
		}
		while(i < width){
			//Set the height downward as a ratio of i to the final width.
			//Determine the distance until the end.
			int temp = width - i;
			//subtract the amplitude from the ratio.
			buffer[i] += (amplitude * (temp / ((float)width/2)));
			i++;
		}
	}
	
	public void makeSawtoothWave(int width, float amplitude){
	int i=0;
		while (i < width){
			buffer[i] += amplitude * ( i / (float)(width));
			i++;
			}
		}

	public void makeFullSineWave(int width, float amplitude) {
		float period = (2 * Mathf.PI / width);
			for (int i=0; i<width; i++) {
				buffer[i] += Mathf.Sin(period * i) * amplitude;
			}
		}
		
	public void makeArcedWave(int width, float amplitude){
		for (int i=0; i<width; i++){
			buffer[i] = Mathf.Sqrt( (Mathf.Pow((float)i, 0.4f)));
		}
	}
		
	//Annoying waves
	
		public void makeHeartbeat(int width, float amplitude){
		int i = 0;
		while (i < (width/2)) {
			//set the height of the buffer upwards as a ratio of the maximum
			buffer[i] += amplitude * (i / ((float)width/2));
			i++;
		}
		while(i < width){
			//Set the height downward as a ratio of i to the final width.
			//Determine the distance until the end.
			int temp = i - width;
			//subtract the amplitude from the ratio.
			buffer[i] += (amplitude * (temp / ((float)width/2)));
			i++;
		}
	}	
	
	
	public bool canMakeWave() {
		for (int i=0; i < numWavePoints-1; i++) {
			if (buffer[i] != 0) {
				return false;
			}
		}
		return true;
	}
	
	public float getAverageHeightOfDangerZone () {
		float sum = 0.0f;
		for (int i = dangerLeft; i <= dangerRight; i++) {
			sum += shiftLeftHeights[i] + shiftRightHeights[i];
		}
		return (sum / dangerWidth);
	}

public void warnOrKill () {
		
		const int countDownLength = 60; // frames
		
		float average = getAverageHeightOfDangerZone();
		if (average  <= 0.25f && average >= -0.25f) {
			// safe
			if (currentDangerState != dangerState.Fine) {
				previousDangerState = currentDangerState;
				currentDangerState = dangerState.Fine;
				dangerCountdown = countDownLength;
			}
		} else if  (average  <= 0.45f && average >= -0.45f) {
			// risky
			if (currentDangerState == dangerState.Bad) {
				// leave it. this way we will only fade red to black if we currently were in Red. 
			} else 	if (currentDangerState != dangerState.Okay) {
				previousDangerState = currentDangerState;
				currentDangerState = dangerState.Okay;
				dangerCountdown = countDownLength;
			}
		} else {
			if (currentDangerState != dangerState.Bad) {
				previousDangerState = currentDangerState;
				currentDangerState = dangerState.Bad;
				dangerCountdown = countDownLength;
			}
			// dead
		}
		//Debug.Log (previousDangerState.ToString() + currentDangerState.ToString());
		
		if (dangerCountdown > 0) {
			if (previousDangerState == dangerState.Fine && currentDangerState == dangerState.Bad) {
				mainCamera.GetComponent<Camera>().backgroundColor = Color.red;
			}
			if (previousDangerState == dangerState.Fine && currentDangerState == dangerState.Okay) {
				mainCamera.GetComponent<Camera>().backgroundColor = Color.yellow;
			}
			if (previousDangerState == dangerState.Okay && currentDangerState == dangerState.Bad) {
				mainCamera.GetComponent<Camera>().backgroundColor = Color.red;
			}
			if (previousDangerState == dangerState.Bad && currentDangerState == dangerState.Okay) {
				mainCamera.GetComponent<Camera>().backgroundColor = Color.yellow;
			}
			if (previousDangerState == dangerState.Okay && currentDangerState == dangerState.Fine) {
				mainCamera.GetComponent<Camera>().backgroundColor = new Color ( ((float) dangerCountdown) / (float) countDownLength, 
																				0.92f * ((float) dangerCountdown) / (float)countDownLength, 
																				0.016f * ((float) dangerCountdown) / (float) countDownLength, 
																				1.0f);
			}
			if (previousDangerState == dangerState.Bad && currentDangerState == dangerState.Fine) {
				mainCamera.GetComponent<Camera>().backgroundColor = new Color ( ((float) dangerCountdown) / (float) countDownLength, 0f, 0f, 1.0f);
			}
			dangerCountdown--;
		}
	}
/*
	public void warnOrKill () {
		
		const int fadeOutLength = 60; // frames
		const int fadeInLength = 10; // frames
		
		float average = getAverageHeightOfDangerZone();
		if (average  <= 0.25f && average >= -0.25f) {
			// safe
			if (currentDangerState != dangerState.Fine) {
				previousDangerState = currentDangerState;
				currentDangerState = dangerState.Fine;
				dangerCountdown = fadeOutLength;
			}
		} else if  (average  <= 0.45f && average >= -0.45f) {
			// risky
			if (currentDangerState == dangerState.Bad) {
				// leave it. this way we will only fade red to black if we currently were in Red. 
			} else 	if (currentDangerState != dangerState.Okay) {
				previousDangerState = currentDangerState;
				currentDangerState = dangerState.Okay;
				dangerCountdown = fadeInLength;
			}
		} else {
			if (currentDangerState != dangerState.Bad) {
				previousDangerState = currentDangerState;
				currentDangerState = dangerState.Bad;
				dangerCountdown = fadeInLength;
			}
			// dead
		}
		//Debug.Log (previousDangerState.ToString() + currentDangerState.ToString());
		
		if (dangerCountdown > 0) {
			if (previousDangerState == dangerState.Fine && currentDangerState == dangerState.Bad) {
				
				mainCamera.GetComponent<Camera>().backgroundColor = new Color ( ((float) dangerCountdown) / (float) fadeInLength, 0f, 0f, 1.0f);
			}
			if (previousDangerState == dangerState.Fine && currentDangerState == dangerState.Okay) {
				
				mainCamera.GetComponent<Camera>().backgroundColor = Color.yellow;
			}
			if (previousDangerState == dangerState.Okay && currentDangerState == dangerState.Bad) {
				mainCamera.GetComponent<Camera>().backgroundColor = new Color ( ((float) dangerCountdown) / (float) fadeInLength, 0f, 0f, 1.0f);
				
			}
			if (previousDangerState == dangerState.Bad && currentDangerState == dangerState.Okay) {
				mainCamera.GetComponent<Camera>().backgroundColor = Color.yellow;
				Debug.Log("Piss off");
			}
			if (previousDangerState == dangerState.Okay && currentDangerState == dangerState.Fine) {
				mainCamera.GetComponent<Camera>().backgroundColor = new Color ( ((float) dangerCountdown) / (float) fadeOutLength, 
																				0.92f * ((float) dangerCountdown) / (float)fadeOutLength, 
																				0.016f * ((float) dangerCountdown) / (float) fadeOutLength, 
																				1.0f);
			}
			if (previousDangerState == dangerState.Bad && currentDangerState == dangerState.Fine) {
				mainCamera.GetComponent<Camera>().backgroundColor = new Color ( ((float) dangerCountdown) / (float) fadeOutLength, 0f, 0f, 1.0f);
			}
			dangerCountdown--;
		}
	}
*/
	
	public void punishAverage () {
		if (shiftLeftHeights[dangerPoint] != 0) {
			buffer[0] += (shiftLeftHeights[dangerPoint] + shiftRightHeights[dangerPoint])/4;
		}
	}
	
	public void cancelDangerZone () {
		//float average = getAverageHeightOfDangerZone ();
		if (shiftLeftHeights[dangerPoint] != 0) {
			shiftLeftHeights[dangerPoint] = shiftLeftHeights[dangerPoint] + shiftRightHeights[dangerPoint];
			shiftRightHeights[dangerPoint] = shiftLeftHeights[dangerPoint] + shiftRightHeights[dangerPoint];
		}
		
	}
	
	public void addScore () {
		float currentScore;
			if (shiftLeftHeights[dangerPoint] != 0f) {
				float waveHeightAtDangerPoint = (shiftRightHeights[dangerPoint] + shiftLeftHeights[dangerPoint]);
				currentScore = (Mathf.Pow(2, (10 * (Mathf.Abs(shiftLeftHeights[dangerPoint]) - Mathf.Abs(waveHeightAtDangerPoint)))));
				score += (int)currentScore;
				if (currentDangerState == dangerState.Bad) {
					score -= 12;
				} else if (currentDangerState == dangerState.Okay) {
					score -= 4;
				}
				if (score < 0) { 
					score = 0; 
				}
			}
			gameUI.transform.FindChild("Panel").transform.FindChild("Text").GetComponent<Text>().text = "Score: " + score;
	}
	
	public void createRandomWave (float maxAmplitude){
		//Create a random amplitude that is larger than 0.04
		float amplitude = Random.value * maxAmplitude;
		//Determine if the wave should be positive of negative amplitude
		amplitude = (Random.value > 0.5) ? amplitude : -amplitude;
		//Determine a random width larger than 40
		float rand = (int)Random.value;
		int width = ((int)(rand * 100)) > 40 ? (int)(rand * 100) : 40;
		//Determine which type of waveform to throw at the player
		float waveType = Random.value;
		if (waveType < 0.333) {
			makeTriangleWave(width, amplitude);
		} else if (waveType < 0.666) {
			makeSineWave(width, amplitude);
		} else {
			makeSquareWave(width, amplitude);
		}
		
	}
	
	public void attenuateWaves(){
		//reduce the right moving waves past the dangerPoint by 40% to be able to see incoming waves easier.
		 shiftRightHeights[dangerPoint + 1] = (shiftRightHeights[dangerPoint + 1] * 0.4f);
		 shiftLeftHeights[dangerPoint - 1] = (shiftLeftHeights[dangerPoint - 1] * 0.4f);	
	}
	
	public void displayMenu(){
	/*
		if (Button[0] == null){
			Debug.Log("Nothing Found");
		}
	
		Button[0].transform.FindChild("Text").GetComponent<Text>().text = "Play";
		Button[1].transform.FindChild("Text").GetComponent<Text>().text = "Tutorial";
		Button[2].transform.FindChild("Text").GetComponent<Text>().text = "Options";
		Button[3].transform.FindChild("Text").GetComponent<Text>().text = "Quit";
		
		Button[0].transform.localPosition = new Vector3(0, 0, -20);
		Button[1].transform.localPosition = new Vector3(0, -40, -20);
		Button[2].transform.localPosition = new Vector3(0, -80, -20);
		Button[3].transform.localPosition = new Vector3(0, -120, -20);
		*/
		}
		
	public void clickPlay(){
		gameState = GameStates.Play;
		score = 0;
		for (int i=0; i<numWavePoints; i++){
			shiftLeftHeights[i] = 0;
			shiftRightHeights[i] = 0;
			buffer[i] = 0;
		}
		updateDifficultySettings();
		mainMenuPage.SetActive(false);
		gameUI.SetActive(true);
		levelFrame = 0;
		currentLevel.currentWaveReading = 0;
		currentWave = null;
	}
	
	public void clickOptions(){
		gameState = GameStates.Options;
		optionsPage.SetActive(true);
		mainMenuPage.SetActive(false);
	}
	
	public void clickHighScores(){
		highscorePage.SetActive(true);
		mainMenuPage.SetActive(false);
		gameState = GameStates.HighScore;
		showHighschores();
	}
	
	public void clickQuit(){
		gameState = GameStates.Quit;
		Application.Quit();
	}
	
	public void clickBack(){
		gameState = GameStates.Menu;
		highscorePage.SetActive(false);
		newHighscorePage.SetActive(false);
		gameUI.SetActive(false);
		mainMenuPage.SetActive(true);
		optionsPage.SetActive(false);
	}
	
	public void togglePause(){
		isPaused = !isPaused;
	}
	
	public void updateDifficultySettings(){
			switch (difficulty) {
			case Difficulties.Tranquil:
				numWavePoints = 500;
				decayFactor = 0.97f;
			break;
			case Difficulties.Lively:
				numWavePoints = 250;
				decayFactor = 0.94f;
			break;
			case Difficulties.Chaotic:
				numWavePoints = 100;
				decayFactor = 0.90f;
			break;
		}
		
		dangerPoint = numWavePoints / 4;
		dangerWidth = numWavePoints * 2 / 100;
		dangerLeft = dangerPoint - dangerWidth / 2;
		dangerRight = dangerPoint + dangerWidth / 2;
		
		shiftRightHeights = new float [numWavePoints];
		shiftLeftHeights = new float [numWavePoints];
		buffer = new float [numWavePoints];
		for (int i = 0; i < numWavePoints; i++) {
				shiftRightHeights[i] = 0;
				shiftLeftHeights[i] = 0;
				buffer[i] = 0;
		}
		LineRenderer lr = lines.GetComponent<LineRenderer>();
		lr.numPositions = numWavePoints-1;
        for (int i = 0 ; i < numWavePoints-1; i++) {
            lr.SetPosition(i, new Vector3(((i * 1.6f / (float) numWavePoints) - 0.8f), shiftLeftHeights[i] + shiftRightHeights[i], 0));
        }
	}
	
	public void showHighschores(){
	
	/*
	PlayerPrefs.SetInt("Highscore1Score", 100);
	PlayerPrefs.SetInt("Highscore2Score", 100);
	PlayerPrefs.SetInt("Highscore3Score", 100);
	PlayerPrefs.SetInt("Highscore4Score", 100);
	PlayerPrefs.SetInt("Highscore5Score", 100);
	PlayerPrefs.SetInt("Highscore6Score", 100);
	PlayerPrefs.SetInt("Highscore7Score", 100);
	PlayerPrefs.SetInt("Highscore8Score", 100);
	PlayerPrefs.SetInt("Highscore9Score", 100);
	PlayerPrefs.SetInt("Highscore10Score", 100);
	
	PlayerPrefs.SetString("Highscore1Name", "");
	PlayerPrefs.SetString("Highscore2Name", "");
	PlayerPrefs.SetString("Highscore3Name", "");
	PlayerPrefs.SetString("Highscore4Name", "");
	PlayerPrefs.SetString("Highscore5Name", "");
	PlayerPrefs.SetString("Highscore6Name", "");
	PlayerPrefs.SetString("Highscore7Name", "");
	PlayerPrefs.SetString("Highscore8Name", "");
	PlayerPrefs.SetString("Highscore9Name", "");
	PlayerPrefs.SetString("Highscore10Name", "");
	*/
	
	
		Text highscoreUsernamesText = highscorePage.transform.FindChild("Panel1").transform.FindChild("UsernameText").GetComponent<Text>();
		Text highscoresScoreText = highscorePage.transform.FindChild("Panel1").transform.FindChild("ScoreText").GetComponent<Text>();

		highscoreUsernamesText.text = "";
		highscoresScoreText.text = "";
		
		for (int i=1; i<=10; i++)
		{
			highscoreUsernamesText.text += "" + i + ". " + PlayerPrefs.GetString(("Highscore"+i+"Name"), "") + "\n";
			highscoresScoreText.text += "" + PlayerPrefs.GetInt(("Highscore"+i+"Score"), 0) + "\n";
		}
	}
	
	public void checkHighscore(){
	int lowestHighscore = PlayerPrefs.GetInt(("Highscore10Score"), 0);
	if (score < lowestHighscore){
		gameUI.SetActive(false);
		mainMenuPage.SetActive(true);
		gameState = GameStates.Menu;
	} else {
		levelFrame = 0;
		currentLevel.currentWaveReading = 0;
		currentWave = null;
		gameUI.SetActive(false);
		newHighscorePage.SetActive(true);
		gameState = GameStates.NewHighscore;
		}
	}

	public void submitHighscore(){
	int checkMe;
	string imBeingChecked;
	for (int i=9; i>=0; i--){
	
		//Load the current highscore and name into the highscore array
		checkMe = PlayerPrefs.GetInt(("Highscore"+(i+1)+"Score"), 0);
		imBeingChecked = PlayerPrefs.GetString(("Highscores"+(i+1)+"Name"), "Player");
	
	
		Debug.Log("Checking you:" + score + " against" + i + ":" + checkMe);
		if (score > checkMe){
			Debug.Log("Continuing search...");
			if (i==0){
				Debug.Log("You're first place!");
				PlayerPrefs.SetInt(("Highscore1Score"), score);
				PlayerPrefs.SetString(("Highscore1Name"), highscoreName.text);
			} else {
				Debug.Log("Replacing score" + (i+1) + "with" + i);
		
				//Replace the bottom score with the one above it.
				PlayerPrefs.SetInt(("Highscore"+(i+1)+"Score"), PlayerPrefs.GetInt(("Highscore"+(i)+"Score"), 0));
				PlayerPrefs.SetString(("Highscore"+(i+1)+"Name"), PlayerPrefs.GetString(("Highscore"+(i)+"Name"), "Player"));
			}
			continue;
		}

			Debug.Log("Putting you at position: " + (i+2));
			//Once your high score can't compare to the beasts above you, put yourself in.
			PlayerPrefs.SetInt(("Highscore"+(i+2)+"Score"), score);
			PlayerPrefs.SetString(("Highscore"+(i+2)+"Name"), highscoreName.text);
		break;
	}
	
	Debug.Log("Moving to Highscore Page");
	levelFrame = 0;
	currentLevel.currentWaveReading = 0;
	currentWave = null;
	newHighscorePage.SetActive(false);
	highscorePage.SetActive(true);
	gameState = GameStates.HighScore;
	showHighschores();
		
	}
	
	public void showTutorialPanel01 () {
		mainMenuPage.SetActive(false);
		tutorialPanels[0].SetActive(true);		
		inTutorial = true;
		currentTutorialState = TutorialStates.inMenu;
	}

	public void showTutorialPanel02 () {
		tutorialPanels[0].SetActive(false);	
		tutorialPanels[1].SetActive(true);	
		currentTutorialState = TutorialStates.inMenu;
		 tutorialFrame = 0;
	}
	public void showTutorialPanel03 () {
		tutorialPanels[1].SetActive(false);	
		tutorialPanels[2].SetActive(true);	
	}
	public void showTutorialPanel04 () {
		tutorialPanels[2].SetActive(false);	
		tutorialPanels[3].SetActive(true);	
		makeSineWave(100, 0.3f);
		currentTutorialState = TutorialStates.waitFirstWave;
		
	}
	public void showTutorialPanel05 () {
		tutorialPanels[3].SetActive(false);	
		tutorialPanels[4].SetActive(true);
		currentTutorialState = TutorialStates.moveWFirstWave;
	}
	public void showTutorialPanel06 () {
		tutorialPanels[4].SetActive(false);	
		tutorialPanels[5].SetActive(true);	
		currentTutorialState = TutorialStates.cancelFirstWave;
	}
	public void showTutorialPanel07 () {
		tutorialPanels[5].SetActive(false);	
		tutorialPanels[6].SetActive(true);
		currentTutorialState = TutorialStates.inMenu;
	}
	public void showTutorialPanel08 () {
		tutorialPanels[6].SetActive(false);	
		tutorialPanels[7].SetActive(true);
		currentTutorialState = TutorialStates.WaitSecondWave;		
	}
	public void showTutorialPanel09 () {
		tutorialPanels[7].SetActive(false);	
		tutorialPanels[8].SetActive(true);	
		makeSineWave(100, 0.3f);
		currentTutorialState = TutorialStates.moveWSecondWave;		
	}
	public void showTutorialPanel10 () {
		tutorialPanels[8].SetActive(false);	
		tutorialPanels[9].SetActive(true);	
		currentTutorialState = TutorialStates.AddFirstWaveY;		
	}
	public void showTutorialPanel11 () {
		tutorialPanels[9].SetActive(false);	
		tutorialPanels[10].SetActive(true);	
		currentTutorialState = TutorialStates.AddFirstWaveR;		
	}
	public void showTutorialPanel12 () {
		tutorialPanels[10].SetActive(false);	
		tutorialPanels[11].SetActive(true);	
		currentTutorialState = TutorialStates.inMenu;		
	}
	public void showTutorialPanel13 () {
		tutorialPanels[11].SetActive(false);	
		tutorialPanels[12].SetActive(true);	
	}	
	public void returnToMain () {
		tutorialPanels[12].SetActive(false);	
		mainMenuPage.SetActive(true);	
		inTutorial = false;
	}	
	
	public bool isFollowing(){
		if (shiftLeftHeights[numWavePoints/2] != 0){
			float mousePos = Input.mousePosition.y / Screen.height - 0.5f;
				if ((mousePos + shiftLeftHeights[numWavePoints/2] < 0.02) && (mousePos + shiftLeftHeights[numWavePoints/2] > -0.02)){
					return true;
				} else {
					return false;
				}		
		} else {
			return true;
		}
	}
	
	public bool isDying(){
		if (shiftLeftHeights[numWavePoints/2] != 0){
			float mousePos = Input.mousePosition.y / Screen.height - 0.5f;
				if ((mousePos - shiftLeftHeights[numWavePoints/2] < 0.02) && (mousePos - shiftLeftHeights[numWavePoints/2] > -0.02)){
					return true;
				} else {
					return false;
				}		
		} else {
			return true;
		}
	}
	
	public void selectTranquilDifficulty(){
		difficulty = Difficulties.Tranquil;
		updateDifficultySettings();
		
		optionsPage.transform.FindChild("Panel").transform.FindChild("TranquilDifficulty").GetComponent<Image>().color = Color.grey;
		optionsPage.transform.FindChild("Panel").transform.FindChild("LivelyDifficulty").GetComponent<Image>().color = new Color32(227, 49, 57, 255);
		optionsPage.transform.FindChild("Panel").transform.FindChild("ChaoticDifficulty").GetComponent<Image>().color = new Color32(227, 49, 57, 255);
	}
	
	public void selectLivelyDifficulty(){
		difficulty = Difficulties.Lively;
		updateDifficultySettings();
		
		optionsPage.transform.FindChild("Panel").transform.FindChild("TranquilDifficulty").GetComponent<Image>().color = new Color32(227, 49, 57, 255);
		optionsPage.transform.FindChild("Panel").transform.FindChild("LivelyDifficulty").GetComponent<Image>().color = Color.grey;
		optionsPage.transform.FindChild("Panel").transform.FindChild("ChaoticDifficulty").GetComponent<Image>().color = new Color32(227, 49, 57, 255);
		
	}
	
	public void selectChaoticDifficulty(){
		difficulty = Difficulties.Chaotic;
		updateDifficultySettings();
		
		optionsPage.transform.FindChild("Panel").transform.FindChild("TranquilDifficulty").GetComponent<Image>().color = new Color32(227, 49, 57, 255);
		optionsPage.transform.FindChild("Panel").transform.FindChild("LivelyDifficulty").GetComponent<Image>().color = new Color32(227, 49, 57, 255);
		optionsPage.transform.FindChild("Panel").transform.FindChild("ChaoticDifficulty").GetComponent<Image>().color = Color.grey;
	}
	
}

