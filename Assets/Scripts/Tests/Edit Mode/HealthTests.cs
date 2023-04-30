using NUnit.Framework;
using NUnit.Framework.Internal.Execution;
using SS3D.Logging;
using SS3D.Systems;
using SS3D.Systems.Health;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace EditorTests
{
    public class HealthTests
    {
        #region Test set up
        [SetUp]
        public void SetUp()
        {

        }

        [TearDown]
        public void TearDown()
        {
        }
        #endregion

        #region StaminaTests

        /// <summary>
        /// Test to confirm that interactions can only be commenced when stamina is greater than zero, and will otherwise fail.
        /// </summary>
        [Test]
        [TestCase(9.99f, true)]
        [TestCase(10.01f, false)]
        public void CanCommenceInteractionOnlyWhenStaminaIsGreaterThatZero(float staminaToDeplete, bool expectedResult)
        {
            IStamina sut = StaminaFactory.Create(10f);

            sut.ConsumeStamina(staminaToDeplete);

            Assert.IsTrue(sut.CanCommenceInteraction == expectedResult);
        }

        /// <summary>
        /// Test to confirm that interactions can be continued when negative. However, they will not continue beyond 10% of the max
        /// stamina in deficit.
        /// </summary>
        [Test]
        [TestCase(10.99f, true)]
        [TestCase(11.01f, false)]
        public void CanContinueInteractionWithNegativeStaminaUntilNegativeTenPercent(float staminaToDeplete, bool expectedResult)
        {
            IStamina sut = StaminaFactory.Create(10f);

            sut.ConsumeStamina(staminaToDeplete);

            Assert.IsTrue(sut.CanContinueInteraction == expectedResult);
        }

        /// <summary>
        /// Test to confirm that reducing stamina will result in the correct value for Current property being returned.
        /// Note that Current should always be in the range of 0 to 1 (inclusive).
        /// </summary>
        [Test]
        [TestCase(0f, 1f)]
        [TestCase(7f, 0.3f)]
        [TestCase(100f, 0f)]
        public void ConsumeStaminaCorrectlyReducesTheStaminaValue(float staminaToDeplete, float expectedResult)
        {
            IStamina sut = StaminaFactory.Create(10f);

            sut.ConsumeStamina(staminaToDeplete);

            Assert.IsTrue(sut.Current == expectedResult);
        }

        /// <summary>
        /// Test to confirm that recharging stamina will result in the correct value for Current property being returned.
        /// Note that Current should always be in the range of 0 to 1 (inclusive).
        /// </summary>
        [Test]
        [TestCase(0f, 0f)]
        [TestCase(0.7f, 0.7f)]
        [TestCase(100f, 1f)]
        public void RechargingStaminaCorrectlyReducesTheStaminaValue(float secondsToRecharge, float expectedResult)
        {
            IStamina sut = StaminaFactory.Create(10f, 1f);   // Set up stamina to fully recharge after 1 second.
            sut.ConsumeStamina(10f);                        // Deplete all of the stamina

            sut.RechargeStamina(secondsToRecharge);

            Assert.IsTrue(sut.Current == expectedResult);
        }
        #endregion

        #region PainTests

        Brain brain;
        HumanBodypart head;
        HumanBodypart torso;
        HumanBodypart leftArm;
        HumanBodypart rightArm;
        HumanBodypart leftLeg;
        HumanBodypart rightLeg;
        HumanBodypart leftHand;
        HumanBodypart rightHand;
        HumanBodypart leftFoot;
        HumanBodypart rightFoot;
        List<BodyPart> BodyParts;

        [SetUp]
        public void SetUpSimpleBody()
        {
            brain = CreateBodyPart<Brain>();
            head = CreateBodyPart<HumanBodypart>();
            torso = CreateBodyPart<HumanBodypart>();
            leftArm = CreateBodyPart<HumanBodypart>();
            rightArm = CreateBodyPart<HumanBodypart>();
            leftLeg = CreateBodyPart<HumanBodypart>();
            rightLeg = CreateBodyPart<HumanBodypart>();
            leftHand = CreateBodyPart<HumanBodypart>();
            rightHand = CreateBodyPart<HumanBodypart>();
            leftFoot = CreateBodyPart<HumanBodypart>();
            rightFoot = CreateBodyPart<HumanBodypart>();

            brain.Init();
            head.Init(brain,"head");
            torso.Init(head, "torso");
            leftArm.Init(torso, "leftArm");
            rightArm.Init(torso, "rightArm");
            leftLeg.Init(torso, "leftLeg");
            rightLeg.Init(torso, "rightLeg");
            leftHand.Init(leftArm, "leftHand");
            rightHand.Init(rightArm, "rightHand");
            leftFoot.Init(leftLeg, "leftFoot");
            rightFoot.Init(rightLeg, "rightFoot");

            BodyParts = new List<BodyPart>() { brain, head, torso, leftArm, rightArm, leftLeg, rightLeg, 
            leftHand, rightHand, leftFoot, rightFoot};
        }

        /// <summary>
        /// Check if body hierarchy is constructed as expected.
        /// </summary>
        [Test]
        public void SimpleBodyHierarchyIsCorrect()
        {
            // child correctly setup.
            Assert.Contains(head, brain.ChildBodyParts);
            Assert.Contains(torso, head.ChildBodyParts);
            Assert.Contains(leftArm, torso.ChildBodyParts);
            Assert.Contains(rightArm, torso.ChildBodyParts);
            Assert.Contains(leftHand, leftArm.ChildBodyParts);
            Assert.Contains(rightHand, rightArm.ChildBodyParts);
            Assert.Contains(leftLeg, torso.ChildBodyParts);
            Assert.Contains(rightLeg, torso.ChildBodyParts);
            Assert.Contains(leftFoot, leftLeg.ChildBodyParts);
            Assert.Contains(rightFoot, rightLeg.ChildBodyParts);

            // parent correctly set up.
            Assert.AreEqual(head.ParentBodyPart, brain);
            Assert.AreEqual(torso.ParentBodyPart, head);
            Assert.AreEqual(leftArm.ParentBodyPart, torso);
            Assert.AreEqual(rightArm.ParentBodyPart, torso);
            Assert.AreEqual(leftLeg.ParentBodyPart, torso);
            Assert.AreEqual(rightLeg.ParentBodyPart, torso);
            Assert.AreEqual(leftHand.ParentBodyPart, leftArm);
            Assert.AreEqual(rightHand.ParentBodyPart, rightArm);
            Assert.AreEqual(leftFoot.ParentBodyPart, leftLeg);
            Assert.AreEqual(rightFoot.ParentBodyPart, rightLeg);
        }


        /// <summary>
        /// Creates an Item without traits
        /// </summary>
        /// <param name="traits"></param>
        /// <returns></returns>
        private static T CreateBodyPart<T>() where T : UnityEngine.Component
        {
            var go = new GameObject();
            var component = go.AddComponent<T>();
            return component;
        }
                      
        #endregion

    }
}
