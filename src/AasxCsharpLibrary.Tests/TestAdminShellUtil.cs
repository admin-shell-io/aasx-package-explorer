using NUnit.Framework;

namespace AdminShellNS.Tests
{
    // ReSharper disable UnusedType.Global
    public class Test_EvalToNonNullString
    {
        [Test]
        public void NonNull_Gives_Formatted()
        {
            var result = AdminShellNS.AdminShellUtil.EvalToNonNullString(
                "some message: {0}", 1984, "something else");

            Assert.That(result, Is.EqualTo("some message: 1984"));
        }

        [Test]
        public void Null_Gives_ElseString()
        {
            var result = AdminShellNS.AdminShellUtil.EvalToNonNullString(
                            "some message: {0}", null, "something else");

            Assert.That(result, Is.EqualTo("something else"));
        }
    }
    // ReSharper restore UnusedType.Global

}
