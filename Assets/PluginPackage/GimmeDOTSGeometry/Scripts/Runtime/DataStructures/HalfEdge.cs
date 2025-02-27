namespace GimmeDOTSGeometry
{
    /// <summary>
    /// A useful edge data structure, that can be used to build polygons, maps, diagrams etc.
    /// </summary>
    public struct HalfEdge
    {
        public int fwd;
        public int back;
        public int vertexFwd;
        public int vertexBack;

        public int twin;

        public HalfEdge(int backwards, int forward, int vertexBack, int vertexFwd, int twin)
        {
            this.fwd = forward;
            this.back = backwards;
            this.vertexFwd = vertexFwd;
            this.vertexBack = vertexBack;
            this.twin = twin;
        }
    }
}
