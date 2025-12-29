using Vector2d = OpenTK.Mathematics.Vector2d;

namespace DxfPad
{
    public interface IDraftConstraintHelper : IDraftHelper
    {
        DraftConstraint Constraint { get; }
        Vector2d SnapPoint { get; set; }


    }
}
