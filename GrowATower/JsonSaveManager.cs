using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Jsons
{
    /// <summary>
    /// Unity용 키-값 기반 JSON 저장/로드 유틸리티
    /// 하나의 파일에 여러 키-값 쌍을 저장하고 관리합니다.
    /// </summary>
    public class JsonSaveManager : Singleton<JsonSaveManager>
    {
        private const string DEFAULT_FILE_NAME = "SaveData.json";
        private SaveDataContainer container;
        private string currentFilePath;

        public JsonSaveManager()
        {
            currentFilePath = Path.Combine(Application.persistentDataPath, DEFAULT_FILE_NAME);
            LoadContainer();
        }

        /// <summary>
        /// 컨테이너 로드 (파일이 없으면 새로 생성)
        /// </summary>
        private void LoadContainer()
        {
            try
            {
                if (File.Exists(currentFilePath))
                {
                    string json = File.ReadAllText(currentFilePath);
                    container = JsonUtility.FromJson<SaveDataContainer>(json);
                    Debug.Log($"[JsonSaveManager] 데이터 로드 완료: {currentFilePath}");
                }
                else
                {
                    container = new SaveDataContainer();
                    Debug.Log("[JsonSaveManager] 새 데이터 컨테이너 생성");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JsonSaveManager] 로드 실패: {ex.Message}");
                container = new SaveDataContainer();
            }
        }

        /// <summary>
        /// 컨테이너 저장
        /// </summary>
        private void SaveContainer()
        {
            try
            {
                string directory = Path.GetDirectoryName(currentFilePath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                string json = JsonUtility.ToJson(container, true);
                File.WriteAllText(currentFilePath, json);
                Debug.Log($"[JsonSaveManager] 저장 완료: {currentFilePath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JsonSaveManager] 저장 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 단일 값 저장 (키의 데이터를 덮어씁니다)
        /// </summary>
        public void Save<T>(string key, T value)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning("[JsonSaveManager] 키가 비어있습니다.");
                return;
            }

            string json = JsonUtility.ToJson(new Wrapper<T> { data = value });

            var existingEntry = container.entries.FirstOrDefault(e => e.key == key);
            if (existingEntry != null)
            {
                // 기존 데이터를 덮어씀
                existingEntry.values = new List<string> { json };
            }
            else
            {
                container.entries.Add(new SaveEntry
                {
                    key = key,
                    values = new List<string> { json }
                });
            }

            SaveContainer();
        }

        /// <summary>
        /// 단일 값 추가 저장 (키에 값을 중복하여 추가합니다)
        /// </summary>
        public void Add<T>(string key, T value)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning("[JsonSaveManager] 키가 비어있습니다.");
                return;
            }

            string json = JsonUtility.ToJson(new Wrapper<T> { data = value });

            var existingEntry = container.entries.FirstOrDefault(e => e.key == key);
            if (existingEntry != null)
            {
                // 기존 리스트에 값을 추가
                existingEntry.values.Add(json);
            }
            else
            {
                container.entries.Add(new SaveEntry
                {
                    key = key,
                    values = new List<string> { json }
                });
            }

            SaveContainer();
        }

        /// <summary>
        /// 여러 값 저장 (키의 데이터를 리스트로 덮어씁니다)
        /// </summary>
        public void SaveList<T>(string key, List<T> values)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning("[JsonSaveManager] 키가 비어있습니다.");
                return;
            }

            List<string> jsonList = new List<string>();
            foreach (var value in values)
            {
                string json = JsonUtility.ToJson(new Wrapper<T> { data = value });
                jsonList.Add(json);
            }

            var existingEntry = container.entries.FirstOrDefault(e => e.key == key);
            if (existingEntry != null)
            {
                // 기존 데이터를 덮어씀
                existingEntry.values = jsonList;
            }
            else
            {
                container.entries.Add(new SaveEntry
                {
                    key = key,
                    values = jsonList
                });
            }

            SaveContainer();
        }

        /// <summary>
        /// 여러 값 추가 저장 (키에 리스트를 중복하여 추가합니다)
        /// </summary>
        public void AddList<T>(string key, List<T> values)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning("[JsonSaveManager] 키가 비어있습니다.");
                return;
            }

            List<string> jsonList = new List<string>();
            foreach (var value in values)
            {
                string json = JsonUtility.ToJson(new Wrapper<T> { data = value });
                jsonList.Add(json);
            }

            var existingEntry = container.entries.FirstOrDefault(e => e.key == key);
            if (existingEntry != null)
            {
                // 기존 리스트에 새로운 리스트의 모든 항목을 추가
                existingEntry.values.AddRange(jsonList);
            }
            else
            {
                container.entries.Add(new SaveEntry
                {
                    key = key,
                    values = jsonList
                });
            }

            SaveContainer();
        }

        /// <summary>
        /// 단일 값 로드 (키에 저장된 첫 번째 값만 반환)
        /// </summary>
        public T Load<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning("[JsonSaveManager] 키가 비어있습니다.");
                return default;
            }

            var entry = container.entries.FirstOrDefault(e => e.key == key);
            if (entry == null || entry.values.Count == 0)
            {
                Debug.LogWarning($"[JsonSaveManager] 키 '{key}'에 해당하는 데이터가 없습니다.");
                return default;
            }

            try
            {
                // 리스트의 첫 번째 항목만 로드
                var wrapper = JsonUtility.FromJson<Wrapper<T>>(entry.values[0]);
                Debug.Log("Load 성공");
                return wrapper.data;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JsonSaveManager] 로드 실패 (키: {key}): {ex.Message}");
                return default;
            }
        }

        /// <summary>
        /// 리스트 로드 (키에 저장된 모든 값을 리스트로 반환)
        /// </summary>
        public List<T> LoadList<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning("[JsonSaveManager] 키가 비어있습니다.");
                return new List<T>();
            }

            var entry = container.entries.FirstOrDefault(e => e.key == key);
            if (entry == null || entry.values.Count == 0)
            {
                Debug.LogWarning($"[JsonSaveManager] 키 '{key}'에 해당하는 데이터가 없습니다.");
                return new List<T>();
            }

            List<T> result = new List<T>();
            // 리스트의 모든 항목을 로드
            foreach (var json in entry.values)
            {
                try
                {
                    var wrapper = JsonUtility.FromJson<Wrapper<T>>(json);
                    result.Add(wrapper.data);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[JsonSaveManager] 리스트 항목 로드 실패: {ex.Message}");
                }
            }

            return result;
        }

        /// <summary>
        /// 키에 저장된 값의 개수 확인
        /// </summary>
        public int GetValueCount(string key)
        {
            var entry = container.entries.FirstOrDefault(e => e.key == key);
            return entry?.values.Count ?? 0;
        }

        /// <summary>
        /// 특정 값 삭제 (모든 키에서 해당 값을 검색하여 삭제)
        /// </summary>
        public void DeleteValue<T>(string key, T value)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning("[JsonSaveManager] 키가 비어있습니다.");
                return;
            }

            var entry = container.entries.FirstOrDefault(e => e.key == key);
            if (entry == null || entry.values.Count == 0)
            {
                Debug.LogWarning($"[JsonSaveManager] 키 '{key}'에 해당하는 데이터가 없습니다.");
                return;
            }

            try
            {
                string targetJson = JsonUtility.ToJson(new Wrapper<T> { data = value });
                int removedCount = entry.values.RemoveAll(json => json == targetJson);

                if (removedCount > 0)
                {
                    // 값이 모두 삭제되면 키도 제거
                    if (entry.values.Count == 0)
                    {
                        container.entries.Remove(entry);
                    }

                    SaveContainer();
                    Debug.Log($"[JsonSaveManager] 키 '{key}'에서 {removedCount}개의 값 삭제 완료");
                }
                else
                {
                    Debug.LogWarning($"[JsonSaveManager] 키 '{key}'에서 일치하는 값을 찾지 못했습니다.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JsonSaveManager] 값 삭제 실패 (키: {key}): {ex.Message}");
            }
        }

        /// <summary>
        /// 특정 키 삭제
        /// </summary>
        public void DeleteKey(string key)
        {
            container.entries.RemoveAll(e => e.key == key);
            SaveContainer();
            Debug.Log($"[JsonSaveManager] 키 삭제 완료: {key}");
        }

        /// <summary>
        /// 모든 데이터 삭제
        /// </summary>
        public void DeleteAll()
        {
            container.entries.Clear();
            SaveContainer();
            Debug.Log("[JsonSaveManager] 모든 데이터 삭제 완료");
        }

        /// <summary>
        /// 특정 키가 존재하는지 확인
        /// </summary>
        public bool HasKey(string key)
        {
            return container.entries.Any(e => e.key == key);
        }

        /// <summary>
        /// 모든 키 목록 반환
        /// </summary>
        public List<string> GetAllKeys()
        {
            return container.entries.Select(e => e.key).ToList();
        }

        [Serializable]
        private class SaveDataContainer
        {
            public List<SaveEntry> entries = new List<SaveEntry>();
        }

        [Serializable]
        private class SaveEntry
        {
            public string key;
            public List<string> values = new List<string>();
        }

        [Serializable]
        private class Wrapper<T>
        {
            public T data;
        }
    }
}