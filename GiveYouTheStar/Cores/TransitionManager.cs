using _01.Develop.LSW._01._Scripts.Manager;
using _01.Develop.LSW._01._Scripts.UI.InGame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets._01.Develop.CDH.Code.Core
{
    public class TransitionManager : MonoSingleton<TransitionManager>
    {
        [SerializeField] private DiagonalStarTransition transition;

        public void NextScene(string sceneName)
        {
            transition.Play(sceneName);
        }
    }
}
