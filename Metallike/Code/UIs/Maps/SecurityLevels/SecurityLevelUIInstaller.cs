using Assets.Work.CDH.Code.Maps;
using UnityEngine;

namespace Assets.Work.CDH.Code.UIs.Maps.SecurityLevels
{
    public class SecurityLevelUIInstaller : MonoBehaviour
    {
        [SerializeField] private SecurityLevelView securityLevelUI;

        public SecurityLevelPresenter Iniitalizer(IMapDataProvider model)
        {
            Debug.Assert(model != null, $"{name}의 SecurityLevelPresenter를 Initializer 함수를 실행하던 중 model이 null입니다");
            Debug.Assert(securityLevelUI != null, $"{name}의 SecurityLevelPresenter를 Initializer 함수를 실행하던 중 securityLevelUI가 null입니다");

            SecurityLevelPresenterInitData data = new()
            {
                Model = model,
                SecurityLevelView = securityLevelUI
            };

            return new(data);
        }
    }
}
