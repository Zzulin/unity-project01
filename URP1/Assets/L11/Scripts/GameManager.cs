using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [SerializeField] string coinPrefix = "Coin_";

    Text _scoreText;
    readonly List<GameObject> _coins = new();
    int _score;

    void Start()
    {
        _scoreText = GameObject.Find("ScoreText")?.GetComponent<Text>();

        CacheCoins();
        UpdateUI("WASD ??,????,? R ??");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void CacheCoins()
    {
        _coins.Clear();
        foreach (var t in FindObjectsOfType<Transform>())
        {
            if (t != null && t.gameObject != null && t.gameObject.name.StartsWith(coinPrefix))
                _coins.Add(t.gameObject);
        }
    }

    public void CollectCoin(GameObject coin)
    {
        if (coin == null || !coin.activeSelf) return;

        coin.SetActive(false);
        _score++;

        if (_score >= _coins.Count)
        {
            UpdateUI($"Win! Score {_score}/{_coins.Count}  ? R ??");
        }
        else
        {
            UpdateUI($"Score {_score}/{_coins.Count}  ? R ??");
        }
    }

    void UpdateUI(string extra)
    {
        if (_scoreText == null) return;
        _scoreText.text = extra;
    }
}