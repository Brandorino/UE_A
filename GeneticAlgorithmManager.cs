using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GeneticAlgorithmManager : MonoBehaviour
{
    public GameObject playerPrefab;
    public int populationSize = 20;
    public int initialActionSequenceLength = 40;
    public int genomeIncreaseInterval = 50;
    public Camera mainCamera;
    public TextMeshProUGUI infoText;
    public float mutationRate = 0.1f;

    private List<GameObject> playerInstances;
    private List<List<bool>> populationGenes;
    private int generationCount = 0;
    private float simulationTime = 100f;
    private float currentSimulationTime = 0f;
    private int currentGeneLength;
    private string playerInfoText = "";

    void Start()
    {
        currentGeneLength = initialActionSequenceLength;
        InitializeFirstGeneration();
        SpawnAllPlayers();
    }

    void Update()
    {
        currentSimulationTime += Time.deltaTime;

        if (AllPlayersDead() || currentSimulationTime >= simulationTime)
        {
            EvolvePopulation();
        }

        UpdateUI();
        FollowBestPlayer();
    }

    private void InitializeFirstGeneration()
    {
        playerInstances = new List<GameObject>();
        populationGenes = new List<List<bool>>();

        for (int i = 0; i < populationSize; i++)
        {
            List<bool> geneSequence = GenerateRandomGeneSequence(currentGeneLength);
            populationGenes.Add(geneSequence);

            GameObject playerObject = Instantiate(playerPrefab, new Vector3(-10.854f, 0.403f, 0f), Quaternion.identity);
            playerObject.SetActive(false);
            playerInstances.Add(playerObject);
        }

        Debug.Log($"Première génération initialisée avec des gènes de longueur {currentGeneLength}");
    }

    private List<bool> GenerateRandomGeneSequence(int length)
    {
        List<bool> sequence = new List<bool>();
        for (int i = 0; i < length; i++)
        {
            sequence.Add(Random.Range(0, 2) == 0);
        }
        return sequence;
    }

    private void SpawnAllPlayers()
    {
        for (int i = 0; i < playerInstances.Count; i++)
        {
            GameObject playerObject = playerInstances[i];
            playerObject.SetActive(true);

            PlayerController playerController = playerObject.GetComponent<PlayerController>();
            if (playerController == null)
            {
                Debug.LogError("Le PlayerController n'a pas été trouvé sur le joueur !");
                continue;
            }

            playerController.Initialize(populationGenes[i], this);
        }

        currentSimulationTime = 0f;
    }

    public void UpdatePlayerInfo(PlayerController player, int currentActionIndex, int totalActions, bool canJump)
    {
        playerInfoText += $"Joueur : {player.gameObject.name}\n";
        playerInfoText += $"Action : {currentActionIndex + 1}/{totalActions}, Peut sauter : {canJump}\n";
        playerInfoText += $"Position : {player.transform.position.x:F2}\n\n";
    }

    private void UpdateUI()
    {
        if (infoText != null)
        {
            int aliveCount = playerInstances.FindAll(p => !p.GetComponent<PlayerController>().IsDead()).Count;

            string info = $"Génération : {generationCount}\n";
            info += $"Taille du gène : {currentGeneLength} actions\n";
            info += $"Joueurs vivants : {aliveCount}/{populationSize}\n\n";
            info += playerInfoText;

            infoText.text = info;
            playerInfoText = "";
        }
    }

    private bool AllPlayersDead()
    {
        foreach (var player in playerInstances)
        {
            if (!player.GetComponent<PlayerController>().IsDead())
            {
                return false;
            }
        }
        return true;
    }

    private void FollowBestPlayer()
    {
        PlayerController bestPlayer = null;
        float maxDistance = float.MinValue;

        foreach (var player in playerInstances)
        {
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController != null && !playerController.IsDead())
            {
                float distance = playerController.GetDistanceTravelled();
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    bestPlayer = playerController;
                }
            }
        }

        if (bestPlayer != null)
        {
            mainCamera.transform.position = new Vector3(bestPlayer.transform.position.x, mainCamera.transform.position.y, mainCamera.transform.position.z);
        }
    }

    private void EvolvePopulation()
    {
        List<PlayerScore> playerScores = new List<PlayerScore>();

        for (int i = 0; i < populationSize; i++)
        {
            float score = playerInstances[i].GetComponent<PlayerController>().GetDistanceTravelled();
            playerScores.Add(new PlayerScore(score, populationGenes[i]));
        }

        playerScores.Sort((a, b) => b.score.CompareTo(a.score));

        List<List<bool>> newPopulationGenes = new List<List<bool>>();
        int survivorsCount = populationSize / 2;

        for (int i = 0; i < survivorsCount; i++)
        {
            newPopulationGenes.Add(new List<bool>(playerScores[i].genes));
            newPopulationGenes.Add(MutateGenes(new List<bool>(playerScores[i].genes)));
        }

        if (generationCount > 0 && generationCount % genomeIncreaseInterval == 0)
        {
            ExtendGeneSequences(newPopulationGenes, 20);
        }

        RestartGeneration(newPopulationGenes);
    }

    private List<bool> MutateGenes(List<bool> genes)
    {
        for (int i = 0; i < genes.Count; i++)
        {
            if (Random.value < mutationRate)
            {
                genes[i] = !genes[i];
            }
        }
        return genes;
    }

    private void ExtendGeneSequences(List<List<bool>> genes, int additionalLength)
    {
        foreach (var geneSequence in genes)
        {
            for (int i = 0; i < additionalLength; i++)
            {
                geneSequence.Add(Random.Range(0, 2) == 0);
            }
        }

        currentGeneLength += additionalLength;
        Debug.Log($"Extension des séquences de gènes à {currentGeneLength} actions.");
    }

    private void RestartGeneration(List<List<bool>> newGenes)
    {
        foreach (var player in playerInstances)
        {
            player.SetActive(false);
            player.GetComponent<PlayerController>().ResetPlayer();
        }

        populationGenes = newGenes;
        generationCount++;
        SpawnAllPlayers();
    }
}

public class PlayerScore
{
    public float score;
    public List<bool> genes;

    public PlayerScore(float score, List<bool> genes)
    {
        this.score = score;
        this.genes = genes;
    }
}
