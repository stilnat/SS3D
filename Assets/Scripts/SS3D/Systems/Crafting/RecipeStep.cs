using NaughtyAttributes;
using SS3D.Data.AssetDatabases;
using System;
using System.Linq;
using UnityEngine;

namespace SS3D.Systems.Crafting
{
    /// <summary>
    /// Represent a single step in a recipe, hold an optionnal result from reaching the step, has a name,
    /// and some data regarding what to do when it's reached.
    /// </summary>
    [Serializable]
    public class RecipeStep
    {
        /// <summary>
        /// The recipe this step belongs to.
        /// </summary>
        [NonSerialized]
        public CraftingRecipe Recipe;

        /// <summary>
        /// The name of the step. Choose it carefully as it is currently how one can refer to it.
        /// </summary>
        [SerializeField]
        private string _name;
        
        /// <summary>
        /// If true, the recipe starts here. There is only one initial step in a recipe. A step can't be terminal and initial at the same time.
        /// </summary>
        [HideIf(nameof(ShowInitialState))]
        [AllowNesting]
        public bool IsInitialState;
        
        /// <summary>
        /// If true, show IsInitialState in the inspector
        /// </summary>
        private bool ShowInitialState => Recipe.HasInitial && !IsInitialState; 

        /// <summary>
        /// If true, the target is consumed (despawned). A step can't be terminal and initial at the same time.
        /// </summary>
        [HideIf(nameof(IsInitialState))]
        [AllowNesting]
        public bool IsTerminal;

        /// <summary>
        /// If true, the result of the recipe step should use a custom craft method, instead of the default one.
        /// Should only be true on a terminal step.
        /// </summary>
        [ShowIf(nameof(IsTerminal))]
        [AllowNesting]
        public bool CustomCraft;

        /// <summary>
        /// A resulting object that will spawn at the end of the crafting process, optional.
        /// </summary>
        [ShowIf(nameof(IsTerminal))]
        [AllowNesting]
        public WorldObjectAssetReference Result;
        
        /// <summary>
        /// Name of the recipe step.
        /// </summary>
        public string Name => _name;

        public RecipeStep(CraftingRecipe recipe, string name)
        {
            Recipe = recipe;
            IsTerminal = false;
            _name = name;
            CustomCraft = false;
            Result = new();
        }
        
        public bool TryGetResult(out WorldObjectAssetReference result)
        {
            result = Result;
            return Result is not null;
        }

        public WorldObjectAssetReference GetResultOrTarget() => Result ? Result : Recipe.Target;
    }
}
