// Core.Tests  Unit tests for GridCell domain logic.

using System;
using NUnit.Framework;

namespace GalacticEmpire.Core.Tests
{
    [TestFixture]
    public sealed class GridCellTests
    {
        [Test]
        public void Empty_CreatesCellWithoutModule()
        {
            var cell = GridCell.Empty(2, 3);

            Assert.That(cell.X, Is.EqualTo(2));
            Assert.That(cell.Y, Is.EqualTo(3));
            Assert.That(cell.IsEmpty, Is.True);
        }

        [Test]
        public void PlaceModule_OnEmptyCell_AddsModuleId()
        {
            var cell   = GridCell.Empty(0, 0);
            var module = Guid.NewGuid();

            var updated = cell.PlaceModule(module);

            Assert.That(updated.IsEmpty, Is.False);
            Assert.That(updated.ModuleId, Is.EqualTo(module));
        }

        [Test]
        public void PlaceModule_OnOccupiedCell_ThrowsInvalidOperationException()
        {
            var cell = GridCell.Empty(0, 0).PlaceModule(Guid.NewGuid());

            Assert.Throws<InvalidOperationException>(() =>
                cell.PlaceModule(Guid.NewGuid()));
        }

        [Test]
        public void RemoveModule_OnOccupiedCell_ClearsModuleId()
        {
            var cell = GridCell.Empty(0, 0).PlaceModule(Guid.NewGuid());

            var updated = cell.RemoveModule();

            Assert.That(updated.IsEmpty, Is.True);
        }

        [Test]
        public void RemoveModule_OnEmptyCell_ThrowsInvalidOperationException()
        {
            var cell = GridCell.Empty(0, 0);

            Assert.Throws<InvalidOperationException>(() =>
                cell.RemoveModule());
        }
    }
}
