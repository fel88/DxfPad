using Vector2d = OpenTK.Mathematics.Vector2d;

namespace DxfPad
{
    public class ChangeCand
    {
        public DraftPoint Point;
        public Vector2d Position;
        public void Apply()
        {
            Point.SetLocation(Position);
        }

    }
}
