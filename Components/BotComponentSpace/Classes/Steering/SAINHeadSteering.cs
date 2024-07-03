namespace SAIN.SAINComponent.Classes.Mover
{
    public class SAINHeadSteering : BotBase, IBotClass
    {
        public SAINHeadSteering(BotComponent bot) : base(bot)
        {

        }

        public void Init()
        {
            base.SubscribeToPreset(null);
        }

        public void Update()
        {

        }

        public void Dispose()
        {
        }
    }
}