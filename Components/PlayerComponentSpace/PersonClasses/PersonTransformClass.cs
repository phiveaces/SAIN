using EFT;
using UnityEngine;

namespace SAIN.Components.PlayerComponentSpace.PersonClasses
{
    public class PersonTransformClass
    {
        private const float TRANSFORM_UPDATE_HEADLOOK_FREQ = 1f / 30f;
        private const float TRANSFORM_UPDATE_VELOCITY_FREQ = 1f / 5f;
        private const float TRANSFORM_MIN_VELOCITY = 0.25f;
        private const float TRANSFORM_MAX_VELOCITY = 5f;

        public Vector3 Position { get; private set; }
        public Vector3 LookDirection { get; private set; }
        public Vector3 HeadLookDirection { get; private set; }
        public Vector3 HeadPosition { get; private set; }
        public Vector3 EyePosition { get; private set; }
        public Vector3 BodyPosition { get; private set; }
        public Vector3 WeaponFirePort { get; private set; }
        public Vector3 WeaponPointDirection { get; private set; }

        public float VelocityMagnitudeNormal { get; private set; }
        public float VelocityMagnitude { get; private set; }
        public Vector3 Velocity { get; private set; }

        public Vector3 DirectionToMe(Vector3 point)
        {
            return Position - point;
        }

        public Vector3 DirectionToPoint(Vector3 point)
        {
            return point - Position;
        }

        public Vector3 Right()
            => AngledLookDirection(0f, 90f, 0f);

        public Vector3 Left()
            => AngledLookDirection(0f, -90f, 0f);

        public Vector3 Back()
            => AngledLookDirection(0f, 180, 0f);

        public Vector3 AngledLookDirection(float x, float y, float z)
            => Quaternion.Euler(x, y, z) * LookDirection;

        public void UpdatePositions()
        {
            updateTransform();
            updateVelocity();
        }

        private void updateTransform()
        {
            Position = _transform.position;
            EyePosition = _eyePart.Center;
            HeadPosition = _myHead.position;
            BodyPosition = _bodyPart.position;
            LookDirection = Person.Player.MovementContext.LookDirection;

            if (_nextUpdateHeadLookTime <= Time.time)
            {
                _nextUpdateHeadLookTime = Time.time + TRANSFORM_UPDATE_HEADLOOK_FREQ;
                //HeadLookDirection = Quaternion.Euler(_myHead.localRotation.y, _myHead.localRotation.x, 0) * _myHead.forward;
                Vector3 headLookDir = Quaternion.Euler(0, _myHead.rotation.x + 90, 0) * _myHead.forward;
                headLookDir.y = LookDirection.y;
                HeadLookDirection = headLookDir;
            }
        }

        private void updateVelocity()
        {
            if (_nextUpdateVelocityTime <= Time.time)
            {
                _nextUpdateVelocityTime = Time.time + TRANSFORM_UPDATE_VELOCITY_FREQ;
                Velocity = Person.Player.MovementContext.Velocity;
                getPlayerVelocity(Velocity.magnitude);
            }
        }

        public PersonTransformClass(PersonClass person)
        {
            Person = person;
            _transform = person.Player.Transform;
            var _bones = person.Player.PlayerBones;
            _myHead = _bones.Head;
            _bodyPart = _bones.Ribcage;
            _eyePart = _bones.BodyPartCollidersDictionary[EBodyPartColliderType.Eyes];
        }

        private void getPlayerVelocity(float magnitude)
        {
            if (magnitude <= TRANSFORM_MIN_VELOCITY)
            {
                VelocityMagnitude = 0f;
                VelocityMagnitudeNormal = 0f;
                return;
            }
            if (magnitude >= TRANSFORM_MAX_VELOCITY)
            {
                VelocityMagnitude = TRANSFORM_MAX_VELOCITY;
                VelocityMagnitudeNormal = 1f;
                return;
            }
            VelocityMagnitude = magnitude;
            float num = TRANSFORM_MAX_VELOCITY - TRANSFORM_MIN_VELOCITY;
            float num2 = magnitude - TRANSFORM_MIN_VELOCITY;
            VelocityMagnitudeNormal = num2 / num;
        }

        private float _nextUpdateHeadLookTime;
        private float _nextUpdateVelocityTime;
        private readonly PersonClass Person;
        private readonly BifacialTransform _transform;
        private readonly BifacialTransform _myHead;
        private readonly BifacialTransform _bodyPart;
        private readonly BodyPartCollider _eyePart;
    }
}