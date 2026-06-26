using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using System.IO;
using System.Linq;

[System.Serializable]
public class Customer
{
    public string id;
    public string _name;
    public int score;
    public string date;
    public string gender;
}

[System.Serializable]
public class CustomerList
{
    public List<Customer> customers = new();
}
[System.Serializable]
public class RankingUI
{
    public TMP_Text name;
    public TMP_Text score;
}


[RequireComponent(typeof(AudioSource))]
public class FallingObjectsGame : MonoBehaviour
{
    [Header("Perguntas")]
    public List<Pergunta> perguntas = new List<Pergunta>();
    public List<Customer> customers = new();
    public List<Customer> topCustomers = new();   // Apenas os 5 primeiros
    private Customer currentCustomer;
    public bool isFemale;
    public GameObject maleAvatar, femaleAvatar;
    string SavePath => Path.Combine(Application.persistentDataPath, "customers.json");
    public bool startGame;

    public TMP_Text perguntaLabel;
    public TMP_Text timerText;
    public TMP_Text scoreText;
    public TMP_Text perguntaNumeroText;
    public TMP_Text rankingLabel;
    public List<RankingUI> rankingUIs = new List<RankingUI>();

    [Header("Prefabs")]
    public List<GameObject> prefabs;

    [Header("Audio")]
    public AudioSource source;
    public AudioSource vozSource;
    public AudioSource musicMenu;
    public AudioSource musicGame;

    [Header("Spawn Area")]
    public RectTransform spawnArea;

    [Header("Spawn")]
    public float spawnInterval = 0.8f;
    public float spawnOffsetY = 100f;

    [Header("Game")]
    public float tempoPergunta = 12f;

    [Header("UI Final")]
    public GameObject endPanel;
    public TMP_Text endScoreText;

    private int perguntaAtual = 0;
    private int score = 0;

    private int votosSim = 0;
    private int votosNao = 0;

    private bool isPlaying = false;
    private float tempoRestantePergunta = 0;
    public UnityEvent OnStart, OnSetUser;

    void Awake()
    {
        LoadCustomers();
    }

    void Start()
    {
        endPanel.SetActive(false);
        musicMenu.Play();        
    }

    public void SaveCustomers()
    {
        customers = customers
            .OrderByDescending(x => x.score)
            .ThenBy(x => x._name)
            .ToList();

        CustomerList list = new CustomerList
        {
            customers = customers
        };

        string json = JsonUtility.ToJson(list, true);
        File.WriteAllText(SavePath, json);
    }

    public void AddCustomer(TMP_InputField input)
    {
        if(string.IsNullOrEmpty(input.text) || string.IsNullOrWhiteSpace(input.text))
        {
            input.text = "<color=red>Digite um nome válido.</color>";
            return;
        }

        currentCustomer = new Customer
        {
            id = System.Guid.NewGuid().ToString(),
            _name = input.text,
            score = 0,
            gender = isFemale ? "Female" : "Male",
            date = System.DateTime.Now.ToString("dd/MM/yyyy HH:mm")
        };

        customers.Add(currentCustomer);

        Start_Game();
    }

    public void SelectGender(bool female)
    {
        isFemale = female;
    }

    public void LoadCustomers()
    {
        if (!File.Exists(SavePath))
        {
            customers = new List<Customer>();
            topCustomers = new List<Customer>();
            SetUICustomers();
            return;
        }

        string json = File.ReadAllText(SavePath);
        CustomerList list = JsonUtility.FromJson<CustomerList>(json);
        customers = list?.customers ?? new List<Customer>();

        customers = customers
            .OrderByDescending(c => c.score)
            .ThenBy(c => c._name)
            .ToList();

        topCustomers = customers.Take(5).ToList();

        SetUICustomers();
    }

    public void SetUICustomers()
    {
        for (int i = 0; i < rankingUIs.Count; i++)
        {
            if (i < topCustomers.Count)
            {
                Customer c = topCustomers[i];

                rankingUIs[i].name.text = c._name;
                rankingUIs[i].score.text = c.score.ToString();
            }
            else
            {
                rankingUIs[i].name.text = "";
                rankingUIs[i].score.text = "";
            }
        }
    }

    IEnumerator StartGame()
    {
        score = 0;

        if (vozSource.clip != null)
        {
            vozSource.Play();
            yield return new WaitWhile(() => vozSource.isPlaying);
        }

        for (int i = 3; i > 0; i--)
        {
            timerText.text = $"Prepare-se: {i}";
            yield return new WaitForSeconds(1f);
        }

        timerText.text = "VAI!";
        yield return new WaitForSeconds(1f);

        musicMenu.Stop();
        musicGame.Play();

        isPlaying = true;

        StartCoroutine(SpawnLoop());
        StartCoroutine(PerguntasLoop());
    }

    public void Start_Game()
    {
        if(!startGame)
        {
            startGame = true;
            femaleAvatar.SetActive(isFemale);
            maleAvatar.SetActive(!isFemale);
            StartCoroutine(StartGame());
            OnStart.Invoke();
            CancelInvoke("ResetAll");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {                            
            OnSetUser.Invoke();
            CancelInvoke("ResetAll");
            Invoke("ResetAll", 30);
        }

        if (!isPlaying)
            return;

        if (tempoRestantePergunta > 0)
        {
            tempoRestantePergunta -= Time.deltaTime;
            timerText.text = $"Tempo: {Mathf.CeilToInt(tempoRestantePergunta)}";
        }

        scoreText.text = $"Acertos: {score}";
    }

    void ResetAll()
    {
        if(!startGame)
        {
            Restart();
        }
    }

    IEnumerator PerguntasLoop()
    {
        while (perguntaAtual < perguntas.Count)
        {
            votosSim = 0;
            votosNao = 0;

            Pergunta pergunta = perguntas[perguntaAtual];
            perguntaLabel.text = pergunta.texto;

            if (perguntaNumeroText != null)
            {
                perguntaNumeroText.text = $"Pergunta {perguntaAtual + 1}/{perguntas.Count}";
            }

            if (pergunta.audio != null)
            {
                vozSource.clip = pergunta.audio;
                vozSource.Play();
            }

            tempoRestantePergunta = tempoPergunta;
            yield return new WaitForSeconds(tempoPergunta);

            bool? respostaEscolhida = null;

            if (votosSim > votosNao)
                respostaEscolhida = true;
            else if (votosNao > votosSim)
                respostaEscolhida = false;

            if (respostaEscolhida.HasValue)
            {
                if (respostaEscolhida.Value == pergunta.respostaCorreta)
                {
                    score += Mathf.Abs(votosSim - votosNao);
                }
            }

            perguntaAtual++;
            yield return new WaitForSeconds(1f);
        }

        EndGame();
    }

    IEnumerator SpawnLoop()
    {
        while (isPlaying)
        {
            SpawnObject();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void SpawnObject()
    {
        if (prefabs.Count == 0 || spawnArea == null)
            return;

        GameObject prefab = prefabs[Random.Range(0, prefabs.Count)];
        Vector2 size = spawnArea.rect.size;
        Vector2 pivot = spawnArea.pivot;

        float minX = -size.x * pivot.x;
        float maxX = size.x * (1 - pivot.x);
        float maxY = size.y * (1 - pivot.y);

        float randomX = Random.Range(minX, maxX);
        float spawnY = Random.Range(maxY, maxY + spawnOffsetY);

        GameObject obj = Instantiate(prefab, spawnArea);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(randomX, spawnY);

        FallingObject fall = obj.GetComponent<FallingObject>();
        if (fall != null)
        {
            fall.manager = this;
        }
    }

    public void RegistrarResposta(bool resposta)
    {
        if (!isPlaying)
            return;

        if (resposta)
            votosSim++;
        else
            votosNao++;
    }

    public void PlaySource(AudioClip clip)
    {
        if (clip != null)
        {
            source.PlayOneShot(clip);
        }
    }

    void EndGame()
    {
        // 1. parar o jogo
        isPlaying = false;
        StopAllCoroutines();

        musicGame.Stop();
        musicMenu.Play();

        perguntaLabel.text = "";
        timerText.text = "Fim de Jogo";

        endPanel.SetActive(true);

        if (currentCustomer != null)
        {
            // 2. atualizar currentCustomer.score
            currentCustomer.score = score;

            // 3. atualizar currentCustomer.date
            currentCustomer.date = System.DateTime.Now.ToString("dd/MM/yyyy HH:mm");

            // 4. salvar JSON
            SaveCustomers();

            // 5. carregar novamente
            LoadCustomers();

            // 6. atualizar currentCustomer
            currentCustomer = customers.Find(x => x.id == currentCustomer.id);

            // 7. descobrir o ranking
            int ranking = customers.FindIndex(x => x.id == currentCustomer.id) + 1;

            // 8. preencher endScoreText
            endScoreText.text =
                $"Ranking: {ranking}º | Nome: {currentCustomer._name} | Pontos: {currentCustomer.score}";
        }

        // 9. chamar Restart()
        Invoke("Restart", 10);
    }

    void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}

[System.Serializable]
public class Pergunta
{
    [TextArea(3, 6)]
    public string texto;

    public AudioClip audio;

    [Tooltip("SIM = true | NÃO = false")]
    public bool respostaCorreta;
}