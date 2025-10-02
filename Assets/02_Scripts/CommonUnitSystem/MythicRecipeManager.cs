// using System.Collections.Generic;
// using System.Linq;
// using UnityEngine;
// using Sirenix.OdinInspector;

// /// <summary>
// /// Manager for mythic merge recipes. Holds all recipe data directly in the inspector.
// /// No ScriptableObject needed - all data is stored as serialized variables.
// /// </summary>
// public class MythicRecipeManager : MonoBehaviour
// {
//     public static MythicRecipeManager Instance { get; private set; }

//     [Title("Mythic Recipe Settings")]
//     [InfoBox("기존 ScriptableObject에서 가져온 5개의 신화 레시피 설정입니다. 각 레시피의 Sequence와 ResultUnit을 Inspector에서 설정하세요.")]
//     [SerializeField] private List<MythicRecipe> recipes = new List<MythicRecipe>();
//     public IEnumerable<MythicRecipe> ActiveRecipes => recipes.Where(r => r != null && r.IsValid());

//     private readonly string mythicRecipeConfigPath = "MythicRecipeConfig";

//     private void Awake()
//     {
//         if (Instance == null)
//         {
//             Instance = this;

//             if (ES3.FileExists(mythicRecipeConfigPath))
//             {
//                 recipes = ES3.Load<List<MythicRecipe>>(mythicRecipeConfigPath);
//             }
//             else
//             {
//                 // 기본 레시피들 초기화
//                 InitializeDefaultRecipes();
//                 ES3.Save(mythicRecipeConfigPath, recipes);
//             }
//         }
//         else
//         {
//             Debug.LogWarning("MythicRecipeManager: Multiple instances detected. Destroying duplicate.");
//             Destroy(gameObject);
//         }
//     }

//     /// <summary> 기본 레시피들을 초기화합니다. </summary>
//     private void InitializeDefaultRecipes()
//     {
//         recipes = new List<MythicRecipe>
//         {
//             new MythicRecipe
//             {
//                 Id = "QueenRat",
//                 Sequence = new List<UnitData>(), // Inspector에서 설정: Mouse + Mouse + Penguin + Mouse + Mouse
//                 ResultUnit = null, // Inspector에서 설정: QueenRat UnitData
//                 OutputCount = 1,
//                 UnlockMessage = "쥐 여왕님이 나타났다!",
//                 isUnlocked = false,
//                 unlockWave = 2
//             },
//             new MythicRecipe
//             {
//                 Id = "Genbu",
//                 Sequence = new List<UnitData>(), // Inspector에서 설정: Turtle + Turtle + Bear + Turtle + Turtle
//                 ResultUnit = null, // Inspector에서 설정: Genbu UnitData
//                 OutputCount = 1,
//                 UnlockMessage = "현무가 각성했다!",
//                 isUnlocked = false,
//                 unlockWave = 10
//             },
//             new MythicRecipe
//             {
//                 Id = "Fenrir",
//                 Sequence = new List<UnitData>(), // Inspector에서 설정: Rhino + Kangaroo + Rhino + Kangaroo + Rhino
//                 ResultUnit = null, // Inspector에서 설정: Fenrir UnitData
//                 OutputCount = 1,
//                 UnlockMessage = "펜리르가 깨어났다!",
//                 isUnlocked = false,
//                 unlockWave = 15
//             },
//             new MythicRecipe
//             {
//                 Id = "SkyGuardian",
//                 Sequence = new List<UnitData>(), // Inspector에서 설정: Penguin + Dragon + Penguin
//                 ResultUnit = null, // Inspector에서 설정: SkyGuardian UnitData
//                 OutputCount = 1,
//                 UnlockMessage = "하늘의 수호자가 나타났다!",
//                 isUnlocked = false,
//                 unlockWave = 20
//             },
//             new MythicRecipe
//             {
//                 Id = "Tridragon",
//                 Sequence = new List<UnitData>(), // Inspector에서 설정: Dragon + Dragon + Dragon + Dragon
//                 ResultUnit = null, // Inspector에서 설정: Tridragon UnitData
//                 OutputCount = 1,
//                 UnlockMessage = "삼두용이 탄생했다!",
//                 isUnlocked = false,
//                 unlockWave = 25
//             }
//         };

//         Debug.Log($"MythicRecipeManager: Initialized {recipes.Count} default recipes.");
//         Debug.Log("Note: UnitData references need to be set in the Inspector for each recipe.");
//     }

//     [Button("저장")]
//     [PropertyOrder(1)]
//     public void SaveMythicRecipeConfig()
//     {
//         ES3.Save(mythicRecipeConfigPath, recipes);
//         Debug.Log($"MythicRecipeManager: {recipes.Count}개의 레시피를 저장했습니다.");
//     }

//     [Button("기본 레시피로 리셋")]
//     [PropertyOrder(2)]
//     public void ResetToDefaultRecipes()
//     {
//         InitializeDefaultRecipes();
//         ES3.Save(mythicRecipeConfigPath, recipes);
//         Debug.Log("MythicRecipeManager: 기본 레시피로 리셋했습니다.");
//     }

//     [Button("레시피 로드")]
//     [PropertyOrder(3)]
//     public void LoadRecipes()
//     {
//         if (ES3.FileExists(mythicRecipeConfigPath))
//         {
//             recipes = ES3.Load<List<MythicRecipe>>(mythicRecipeConfigPath);
//             Debug.Log($"MythicRecipeManager: {recipes.Count}개의 레시피를 로드했습니다.");
//         }
//         else
//         {
//             Debug.LogWarning("MythicRecipeManager: 저장된 레시피 파일이 없습니다.");
//         }
//     }
// }

// /// <summary>
// /// Individual mythic recipe as a serializable class for easy management in the inspector.
// /// Each recipe defines a sequence of units that can be merged into a mythic unit.
// /// </summary>
// [System.Serializable]
// public class MythicRecipe
// {
//     [HorizontalGroup("Recipe")]
//     [LabelWidth(60)]
//     [Tooltip("레시피 식별자")] 
//     public string Id;
    
//     [HorizontalGroup("Recipe")]
//     [LabelWidth(80)]
//     [Tooltip("결과 유닛 개수")] 
//     public int OutputCount = 1;

//     [Title("조합 순서")]
//     [InfoBox("유닛들이 정확히 이 순서대로 나타나야 합니다")]
//     [Tooltip("필요한 유닛들의 순서")] 
//     public List<UnitData> Sequence = new List<UnitData>();
    
//     [Title("결과")]
//     [Tooltip("생성될 신화 유닛")] 
//     public UnitData ResultUnit;

//     [Title("해금 설정")]
//     [HorizontalGroup("Unlock")]
//     [LabelWidth(80)]
//     [Tooltip("이 신화 유닛이 해금되었는지 여부")]
//     public bool isUnlocked = false;
    
//     [HorizontalGroup("Unlock")]
//     [LabelWidth(80)]
//     [Tooltip("이 신화 유닛이 해금되는 웨이브 번호")]
//     public int unlockWave = 5;

//     [Title("메시지")]
//     [Tooltip("해금 시 표시될 메시지")] 
//     public string UnlockMessage = string.Empty;

//     /// <summary>
//     /// Validates that this recipe has all required data
//     /// </summary>
//     public bool IsValid()
//     {
//         return !string.IsNullOrEmpty(Id) && 
//                ResultUnit != null && 
//                Sequence != null && 
//                Sequence.Count > 0 && 
//                Sequence.All(unit => unit != null);
//     }

//     /// <summary>
//     /// Gets a human-readable description of this recipe
//     /// </summary>
//     public string GetDescription()
//     {
//         if (!IsValid()) return "Invalid Recipe";
        
//         var sequenceNames = Sequence.Select(unit => unit.name).ToArray();
//         return $"{Id}: {string.Join(" + ", sequenceNames)} → {ResultUnit.name}";
//     }
// }
