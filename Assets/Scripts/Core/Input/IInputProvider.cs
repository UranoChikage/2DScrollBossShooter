using System;
using UnityEngine;

/// <summary>
/// Move, Fireheld, OnBombPressed, OnCrouchedを提供するインターフェース
/// InputActionの値をこのインターフェースに、読み込む側はこのインターフェースを通して値を取得する
/// </summary>
public interface IInputProvider
{
    Vector2 Move { get; }
    bool Fireheld { get; }
    event Action OnBombPressed;
    event Action OnCrouched;//しゃがんだタイミングで呼ばれる。
    Device lastDevice { get; }
}
