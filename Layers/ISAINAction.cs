using System.Collections;

namespace SAIN.Layers
{
    public interface ISAINAction
    {
        void Toggle(bool value);

        IEnumerator ActionCoroutine();
    }
}
