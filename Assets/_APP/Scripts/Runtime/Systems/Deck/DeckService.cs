using App.Gameplay;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// デッキの生成・ドローを司るサービス。
/// - 生成時に DeckDefinition のエントリから重み付きでランダム作成
/// - 「初手2の保証」
/// - 「オープニング・ガード（最初 N ドローは <= openingMaxValue）」
/// - 空になったら再構築（reshuffleWhenEmpty）
/// </summary>
public class DeckService : MonoBehaviour
{
    [Header("Definition")]
    [SerializeField] private DeckDefinition definition;

    // 実デッキ
    private readonly Queue<int> _deck = new Queue<int>(128);

    // 乱数
    private System.Random _rng;

    // 初手2の保証フラグ
    private bool _firstDrawDone = false;

    // 何枚引いたか（オープニング・ガード用）
    private int _drawnCount = 0;

    /// <summary>残枚数（旧実装互換用）</summary>
    public int Remaining => _deck.Count;
    /// <summary>残枚数（別名）</summary>
    public int RemainingCount => _deck.Count;

    private void Awake()
    {
        Build();
    }

    /// <summary>
    /// デッキを再構築（残りをクリアして新しく積み直し）
    /// </summary>
    public void Build()
    {
        if (definition == null)
        {
            Debug.LogWarning("[DeckService] DeckDefinition が設定されていません。暫定で 2 のみで生成します。");
        }

        _deck.Clear();
        _firstDrawDone = false;
        _drawnCount = 0;

        // 乱数初期化
        int seed = 0;
        if (definition != null && definition.fixedSeed != 0)
            seed = definition.fixedSeed;
        else
            seed = Environment.TickCount ^ (int)DateTime.UtcNow.Ticks;

        _rng = new System.Random(seed);

        // 初期枚数ぶん重み付きで詰む
        int count = (definition != null) ? Mathf.Max(0, definition.initialCount) : 42;

        for (int i = 0; i < count; i++)
        {
            int v = WeightedPick(definition);
            _deck.Enqueue(v);
        }
    }

    /// <summary>
    /// 1 枚引く。成功すれば true。
    /// - 初手 2 の保証
    /// - オープニング・ガード（最初 N ドローは <= openingMaxValue）
    /// - 空で reshuffleWhenEmpty が ON なら Build() して続行
    /// </summary>
    public bool TryDraw(out int value)
    {
        value = 0;

        // 初手保証：firstCardValue を返す（枚数は 1 枚消費）
        if (definition != null && definition.guaranteeFirstIsTwo && !_firstDrawDone)
        {
            _firstDrawDone = true;

            if (!ConsumeOneFromDeck()) return false;  // 山が無ければ失敗

            value = definition.firstCardValue;
            _drawnCount++;
            return true;
        }

        // 通常ドロー
        if (_deck.Count == 0)
        {
            if (definition != null && definition.reshuffleWhenEmpty)
            {
                Build();
            }
            else
            {
                return false;
            }
        }

        int v = _deck.Dequeue();

        // オープニング・ガード：最初 openingDraws 回は openingMaxValue 以下になるまで引き直し
        if (definition != null && _drawnCount < definition.openingDraws)
        {
            int guard = 32; // 無限ループ保険
            while (v > definition.openingMaxValue && guard-- > 0)
            {
                if (_deck.Count == 0)
                {
                    if (definition.reshuffleWhenEmpty) Build();
                    else { value = 0; return false; }
                }
                v = _deck.Dequeue();
            }
        }

        value = v;
        _drawnCount++;
        return true;
    }

    /// <summary>次の値を覗き見（空なら -1）</summary>
    public int PeekNext()
    {
        if (_deck.Count == 0) return -1;
        return _deck.Peek();
    }

    /// <summary>山から 1 枚だけ消費（値は見ない）。空なら reshuffle 対応。</summary>
    private bool ConsumeOneFromDeck()
    {
        if (_deck.Count == 0)
        {
            if (definition != null && definition.reshuffleWhenEmpty)
            {
                Build();
            }
            else
            {
                return false;
            }
        }

        if (_deck.Count == 0) return false;
        _deck.Dequeue();
        return true;
    }

    /// <summary>
    /// DeckDefinition.entries から重み付きで 1 つ選ぶ。
    /// entries の構造は { value:int, weight:int } を想定。
    /// </summary>
    private int WeightedPick(DeckDefinition def)
    {
        // 定義が無い場合のフェイルセーフ：常に 2
        if (def == null || def.entries == null || def.entries.Length == 0)
            return 2;

        // 合計重み
        int total = 0;
        for (int i = 0; i < def.entries.Length; i++)
        {
            int w = Math.Max(0, def.entries[i].weight);
            total += w;
        }
        if (total <= 0) return def.entries[0].value; // すべて 0 のとき

        // 0..total-1 の一様乱数で累積に突っ込む
        int r = _rng.Next(total);
        int acc = 0;
        for (int i = 0; i < def.entries.Length; i++)
        {
            int w = Math.Max(0, def.entries[i].weight);
            acc += w;
            if (r < acc) return def.entries[i].value;
        }
        return def.entries[def.entries.Length - 1].value; // 保険
    }

}
