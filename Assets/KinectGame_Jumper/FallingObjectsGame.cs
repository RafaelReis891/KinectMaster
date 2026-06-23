using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

[RequireComponent(typeof(AudioSource))]
public class FallingObjectsGame : MonoBehaviour
{
    [Header("Perguntas")]
    public List<Pergunta> perguntas = new List<Pergunta>();

    public TMP_Text perguntaLabel;
    public TMP_Text timerText;
    public TMP_Text scoreText;
    public TMP_Text perguntaNumeroText;

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

    void Start()
    {
        endPanel.SetActive(false);

        musicMenu.Play();

        StartCoroutine(StartGame());
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

    void Update()
    {
        if (!isPlaying)
            return;

        if (tempoRestantePergunta > 0)
        {
            tempoRestantePergunta -= Time.deltaTime;

            timerText.text =
                $"Tempo: {Mathf.CeilToInt(tempoRestantePergunta)}";
        }

        scoreText.text = $"Acertos: {score}";
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
                perguntaNumeroText.text =
                    $"Pergunta {perguntaAtual + 1}/{perguntas.Count}";
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

        GameObject prefab =
            prefabs[Random.Range(0, prefabs.Count)];

        Vector2 size = spawnArea.rect.size;
        Vector2 pivot = spawnArea.pivot;

        float minX = -size.x * pivot.x;
        float maxX = size.x * (1 - pivot.x);

        float maxY = size.y * (1 - pivot.y);

        float randomX = Random.Range(minX, maxX);
        float spawnY = Random.Range(maxY, maxY + spawnOffsetY);

        GameObject obj =
            Instantiate(prefab, spawnArea);

        RectTransform rect =
            obj.GetComponent<RectTransform>();

        rect.anchoredPosition =
            new Vector2(randomX, spawnY);

        FallingObject fall =
            obj.GetComponent<FallingObject>();

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
        isPlaying = false;

        StopAllCoroutines();

        musicGame.Stop();
        musicMenu.Play();

        perguntaLabel.text = "";
        timerText.text = "Fim de Jogo";

        endPanel.SetActive(true);
        endScoreText.text =
            $"Você obteve {score} pontos!";
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