using QueryBuilderStarter;

namespace QueryBuilderStarter
{
    public interface IClassModel
    {
        public int Id { get; set; }
    }

    public class Pokemon : IClassModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class BannedGame : IClassModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
    }

}