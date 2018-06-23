using loggerApp.Models;
using loggerApp.Queue;
using System.Collections.Generic;

namespace loggerApp.Producers
{
    internal class RecipeList: IQueueingData
    {
        public string Name { get; set; }
        public List<Recipe> Recipes { get; set; }

        internal RecipeList()
        {
            Recipes = new List<Recipe>();
        }
    }
}
