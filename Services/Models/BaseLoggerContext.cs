using loggerMigrator.Migrations;
using System.ComponentModel;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;

namespace loggerApp.Models
{
    public class BaseLoggerContext : DbContext
    {

        public BaseLoggerContext()
        {
            Database.SetInitializer<BaseLoggerContext>(null);
        }
        public DbSet<Recipe> Recipes { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            var convention = new AttributeToColumnAnnotationConvention<DefaultValueAttribute, string>(MyCodeGenerator.SqlDefaultValue, (p, attributes) => attributes.SingleOrDefault().Value.ToString());
            modelBuilder.Conventions.Add(convention);
        }
        //Logger 設定 データは行うため他のプロジェクトから自動Migration
    }
}
