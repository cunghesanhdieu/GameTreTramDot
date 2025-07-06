using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public enum GameState
{
    START,INPUT,GROWING,NONE
}

public class GameManager : MonoBehaviour
{    

    [SerializeField]
    private Vector3 startPos;

    [SerializeField]
    private Vector2 minMaxRange,spawnRange;

    [SerializeField]
    private GameObject pillarPrefab, playerPrefab, stickPrefab, trePrefab, currentCamera;

    [SerializeField]
    private Transform rotateTransform, endRotateTransform;

    [SerializeField]
    private GameObject scorePanel, startPanel, endPanel;

    [SerializeField]
    private TMP_Text scoreText, scoreEndText, tresText, highScoreText;

    private GameObject currentPillar, nextPillar, currentStick, player; 

    private int score, tres, highScore;

    private float cameraOffsetX;

    private GameState currentState;

    [SerializeField]
    private float stickIncreaseSpeed, maxStickSize;

    public static GameManager instance;

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        currentState = GameState.START;

        endPanel.SetActive(false);
        scorePanel.SetActive(false);
        startPanel.SetActive(true);

        score = 0;
        tres = PlayerPrefs.HasKey("Tres") ? PlayerPrefs.GetInt("Tres") : 0;
        highScore = PlayerPrefs.HasKey("HighScore") ? PlayerPrefs.GetInt("HighScore") : 0;

        scoreText.text = score.ToString();
        tresText.text = tres.ToString();
        highScoreText.text = highScore.ToString();

        CreateStartObjects();
        cameraOffsetX = currentCamera.transform.position.x - player.transform.position.x;
        if (StateManager.instance.hasSceneStarted)
        {
            GameStart();
        }

    }

    private void Update()
    {
        if(currentState == GameState.INPUT)
        {
            if(Input.GetMouseButton(0))
            {
                currentState =GameState.GROWING;
                ScaleStick();
            }
        }

        if(currentState == GameState.GROWING)
        {
            if(Input.GetMouseButton(0))
            {
                ScaleStick();
            }
            else
            {
                StartCoroutine(FallStick());
            }
        }
    }

    void ScaleStick()
    {
        Vector3 tempScale = currentStick.transform.localScale;
        tempScale.y += Time.deltaTime * stickIncreaseSpeed;
        if(tempScale.y > maxStickSize)
            tempScale.y = maxStickSize;
        currentStick.transform.localScale = tempScale;
    }

    IEnumerator FallStick()
    {
        currentState = GameState.NONE;
        var x = Rotate(currentStick.transform, rotateTransform, 0.4f);
        yield return x;
        

        Vector3 movePosition = currentStick.transform.position + new Vector3(currentStick.transform.localScale.y,0,0);
        movePosition.y = player.transform.position.y;
        x = Move(player.transform,movePosition,0.5f);
        yield return x;

        var results = Physics2D.RaycastAll(player.transform.position,Vector2.down);
        var result = Physics2D.Raycast(player.transform.position, Vector2.down);
        foreach (var temp in results)
        {
            if(temp.collider.CompareTag("Platform"))
            {
                result = temp;
            }
        }

        if(!result || !result.collider.CompareTag("Platform"))
        {
            player.GetComponent<Rigidbody2D>().gravityScale = 1f;
            x = Rotate(currentStick.transform, endRotateTransform, 0.5f);
            yield return x;
            GameOver();
        }
        else
        {
            UpdateScore();

            movePosition = player.transform.position;
            movePosition.x = nextPillar.transform.position.x + nextPillar.transform.localScale.x * 0.5f - 0.35f;
            x = Move(player.transform, movePosition, 0.2f);
            yield return x;

            movePosition = currentCamera.transform.position;
            movePosition.x = player.transform.position.x + cameraOffsetX;
            x = Move(currentCamera.transform, movePosition, 0.5f);
            yield return x;

            CreatePlatform();
            SetRandomSize(nextPillar);
            currentState = GameState.INPUT;
            Vector3 stickPosition = currentPillar.transform.position;
            stickPosition.x += currentPillar.transform.localScale.x * 0.5f - 0.05f;
            stickPosition.y = currentStick.transform.position.y;
            stickPosition.z = currentStick.transform.position.z;
            currentStick = Instantiate(stickPrefab, stickPosition, Quaternion.identity);
        }
    }


    void CreateStartObjects()
    {
            CreatePlatform();
            Vector3 playerPos = playerPrefab.transform.position;
            playerPos.x += (currentPillar.transform.localScale.x * 0.5f - 0.35f);
            player = Instantiate(playerPrefab,playerPos,Quaternion.identity);
            player.name = "Player";

            Vector3 stickPos = stickPrefab.transform.position;
            stickPos.x += (currentPillar.transform.localScale.x*0.5f - 0.05f);
            currentStick = Instantiate(stickPrefab, stickPos, Quaternion.identity);
    }

        void CreatePlatform()
        {
            var currentPlatform = Instantiate(pillarPrefab);
            currentPillar = nextPillar == null ? currentPlatform : nextPillar;
            nextPillar = currentPlatform;
            nextPillar.transform.position = currentPillar.transform.position + startPos;
            Vector3 tempDistance = new Vector3(Random.Range(spawnRange.x, spawnRange.y) + currentPillar.transform.localScale.x*0.5f,0,0);
            startPos += tempDistance;  

            if(Random.Range(0,10) == 0)
            {
                var tempTre = Instantiate(trePrefab);
                Vector3 tempPos = currentPlatform.transform.position;
                tempPos.y = trePrefab.transform.position.y;
                tempTre.transform.position = tempPos;
            }    
        }

        void SetRandomSize(GameObject pillar)
        {
            var newScale = pillar.transform.localScale;
            var allowedScale = nextPillar.transform.position.x - currentPillar.transform.position.x - currentPillar.transform.localScale.x*0.5f - 0.4f;
            newScale.x = Mathf.Max(minMaxRange.x,Random.Range(minMaxRange.x,Mathf.Min(allowedScale,minMaxRange.y)));

            pillar.transform.localScale = newScale;
        }

        void UpdateScore()
        {
            score++;
            scoreText.text = score.ToString();

            if(score > highScore)
            {
                highScore = score;
                PlayerPrefs.SetInt("HighScore", highScore);

            }

            scoreEndText.text = score.ToString();
            highScoreText.text = highScore.ToString();
        }

        void GameOver()
        {
            endPanel.SetActive(true);
            scorePanel.SetActive(false);
        }

        public void UpdateTres()
        {
            tres++;
            PlayerPrefs.SetInt("Tres", tres);
            tresText.text = tres.ToString();
        }

        public void GameStart()
        {
            startPanel.SetActive(false);
            scorePanel.SetActive(true);

            CreatePlatform();
            SetRandomSize(nextPillar);
            currentState = GameState.INPUT;

        }

        public void GameRestart()
        {
            StateManager.instance.hasSceneStarted = true;
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }    
    
    
      //Helper Functions
    IEnumerator Move(Transform currentTransform,Vector3 target,float time)
    {
        var passed = 0f;
        var init = currentTransform.transform.position;
        while(passed < time)
        {
            passed += Time.deltaTime;
            var normalized = passed / time;
            var current = Vector3.Lerp(init, target, normalized);
            currentTransform.position = current;
            yield return null;
        }
    }

    IEnumerator Rotate(Transform currentTransform, Transform target, float time)
    {
        var passed = 0f;
        var init = currentTransform.transform.rotation;
        while (passed < time)
        {
            passed += Time.deltaTime;
            var normalized = passed / time;
            var current = Quaternion.Slerp(init, target.rotation, normalized);
            currentTransform.rotation = current;
            yield return null;
        }
    }
}