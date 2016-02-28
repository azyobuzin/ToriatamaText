namespace ToriatamaText
{
    public struct EntityInfo
    {
        public int Start { get; }
        public int Length { get; }
        public EntityType Type { get; }

        public EntityInfo(int start, int length, EntityType type)
        {
            this.Start = start;
            this.Length = length;
            this.Type = type;
        }
    }
}
