using NUnit.Framework;
using GameVM.Compiler.Core.Enums;
using GameVM.Compiler.Core.Attributes;
using GameVM.Compiler.Core;
using System.Collections.Generic;

namespace GameVM.Compiler.Core.Tests
{
    [TestFixture]
    public class CapabilityValidationTests
    {
        [Test]
        public void RequiresCapabilityAttribute_ShouldStoreLevel()
        {
            var attr = new RequiresCapabilityAttribute(CapabilityLevel.L3);
            Assert.That(attr.Level, Is.EqualTo(CapabilityLevel.L3));
        }

        [Test]
        public void Validator_ShouldDetectViolation_WhenProfileIsLowerThanRequired()
        {
            var projectProfile = CapabilityLevel.L1;
            var requiredLevel = CapabilityLevel.L2;

            var validator = new CapabilityValidator(projectProfile);

            bool isAllowed = validator.IsAllowed(requiredLevel);

            Assert.That(isAllowed, Is.False, "L2 feature should NOT be allowed on L1 profile");
        }

        [Test]
        public void Validator_ShouldAllow_WhenProfileIsEqualOrHigher()
        {
            var validator = new CapabilityValidator(CapabilityLevel.L3);

            Assert.That(validator.IsAllowed(CapabilityLevel.L3), Is.True, "L3 feature should be allowed on L3 profile");
            Assert.That(validator.IsAllowed(CapabilityLevel.L1), Is.True, "L1 feature should be allowed on L3 profile");
        }

        [Test]
        public void Validator_ShouldAllow_WhenExtensionIsPresent()
        {
            var profile = CapabilityLevel.L1;
            var requiredLevel = CapabilityLevel.L5;
            var extensionId = "Ext.Snd.Polyphonic";

            var validator = new CapabilityValidator(profile, new[] { extensionId });

            bool isAllowed = validator.IsAllowed(requiredLevel, extensionId);

            Assert.That(isAllowed, Is.True, "L5 feature should be allowed on L1 if the specific extension is present");
        }
    }
}
