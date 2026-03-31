using Core.EventBus;
using System;
using UnityEngine;

public class BusManager : Singleton<BusManager>
{
    /// <summary>
    /// 이벤트에 변수 없으면 이 함수 사용
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public void SendEvent<T>() where T : IEvent, new() => TrySendEvent(new T());

    /// <summary>
    /// 이벤트로 보낼 값(변수) 있으면 이 함수 사용
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="eventInstance"></param>
    public void SendEvent<T>(T eventInstance) where T : IEvent => TrySendEvent(eventInstance);

    private void TrySendEvent<T>(T eventInstance) where T : IEvent
    {
        var onEvent = Bus<T>.OnEvent;

        if (onEvent == null) return; // 구독자 없으면 빠른 리턴
         
        Delegate[] subscribers = onEvent.GetInvocationList(); // 구독자들 배열로 빼기

        foreach (Delegate subscriber in subscribers)
        {
            if (subscriber.Target is UnityEngine.Object unityObject)
            {
                // 메모리엔 존재하지만 Destroy된 객체인 경우
                if (unityObject == null)
                {
                    string leakClassName = subscriber.Method.DeclaringType?.Name ?? "Unknown";
                    Debug.LogWarning($"[메모리 누수 감지] 이미 파괴된 '{leakClassName}' 오브젝트가 '{typeof(T).Name}' 이벤트를 여전히 구독하고 있습니다.\n해당 스크립트에서 이벤트 해지가 되어있지 않습니다.");

                    // 강제 이벤트 해지
                    Bus<T>.OnEvent -= (Bus<T>.Event)subscriber;

                    // 다음
                    continue;
                }
            }

            try
            {
                var action = (Bus<T>.Event)subscriber; // 기존 Event타입으로 캐스팅 후 발행
                action.Invoke(eventInstance);
            }
            catch (Exception ex)
            {
                string targetClassName = subscriber.Method.DeclaringType?.Name ?? "UnknownClass";
                string targetMethodName = subscriber.Method.Name;

                string errorMessage =
                    $"[이벤트 에러] {typeof(T).Name} 전송 중 예외 발생!\n" +
                    $"터진 곳: {targetClassName}.{targetMethodName}()\n" +
                    $"메시지: {ex.Message}\n" +
                    $"호출 스택: {ex.StackTrace}";

                Debug.LogError(errorMessage);
            }
        }
    }
}
