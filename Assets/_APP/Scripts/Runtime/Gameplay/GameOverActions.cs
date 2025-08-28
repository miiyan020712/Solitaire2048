// GameOverActions.cs
using UnityEngine;
using App.Gameplay;   // DeckController / BoardPlacer がこの名前空間なら残す。無ければ消してOK。
using App.UI;      // 無いなら消してOK

public class GameOverActions : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] DeckController   deck;
    [SerializeField] UIDragGhost      drag;
    [SerializeField] UITapAutoSuggest tap;
    [SerializeField] BoardPlacer      board;
    [SerializeField] GameObject       gameOverPanel;
    [SerializeField] TimeBarController timeBar;

    [Header("Options")]
    [SerializeField] bool pauseTimeScale = false;

    void Awake()
    {
        if (gameOverPanel) gameOverPanel.SetActive(false); // 初期は非表示
    }

    // ← RowSpawner の On Game Over にこれを割り当てる
    public void OnGameOver()
    {
        if (deck)  deck.LockDraw();
        if (drag)  drag.SetEnabled(false);
        if (tap)   tap.SetEnabled(false);
        if (board) board.SetLocked(true);
        if (gameOverPanel) gameOverPanel.SetActive(true);
        if (pauseTimeScale) Time.timeScale = 0f;
        if (timeBar) timeBar.Pause(true);
    }

    // 既に Run を配線しちゃってた人用のエイリアス
    public void Run() => OnGameOver();
}
