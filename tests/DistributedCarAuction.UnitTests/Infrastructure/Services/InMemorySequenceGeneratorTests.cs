namespace DistributedCarAuction.UnitTests.Infrastructure.Services;

using DistributedCarAuction.Infrastructure.Services;
using FluentAssertions;
using Xunit;

public class InMemorySequenceGeneratorTests
{
    private readonly InMemorySequenceGenerator _generator;

    public InMemorySequenceGeneratorTests()
    {
        _generator = new InMemorySequenceGenerator();
    }

    #region GetNextSequence Tests

    [Fact]
    public void GetNextSequence_FirstCall_ReturnsOne()
    {
		// Arrange
		Guid lotId = Guid.NewGuid();

		// Act
		long sequence = _generator.GetNextSequence(lotId);

        // Assert
        sequence.Should().Be(1);
    }

    [Fact]
    public void GetNextSequence_MultipleCalls_ReturnsIncreasingSequence()
    {
		// Arrange
		Guid lotId = Guid.NewGuid();

		// Act
		long seq1 = _generator.GetNextSequence(lotId);
		long seq2 = _generator.GetNextSequence(lotId);
		long seq3 = _generator.GetNextSequence(lotId);

        // Assert
        seq1.Should().Be(1);
        seq2.Should().Be(2);
        seq3.Should().Be(3);
    }

    [Fact]
    public void GetNextSequence_DifferentLots_IndependentSequences()
    {
		// Arrange
		Guid lotId1 = Guid.NewGuid();
		Guid lotId2 = Guid.NewGuid();

		// Act
		long lot1Seq1 = _generator.GetNextSequence(lotId1);
		long lot1Seq2 = _generator.GetNextSequence(lotId1);
		long lot2Seq1 = _generator.GetNextSequence(lotId2);
		long lot1Seq3 = _generator.GetNextSequence(lotId1);
		long lot2Seq2 = _generator.GetNextSequence(lotId2);

        // Assert
        lot1Seq1.Should().Be(1);
        lot1Seq2.Should().Be(2);
        lot1Seq3.Should().Be(3);
        lot2Seq1.Should().Be(1);
        lot2Seq2.Should().Be(2);
    }

    #endregion

    #region GetNextSequenceAsync Tests

    [Fact]
    public async Task GetNextSequenceAsync_FirstCall_ReturnsOne()
    {
		// Arrange
		Guid lotId = Guid.NewGuid();

		// Act
		long sequence = await _generator.GetNextSequenceAsync(lotId);

        // Assert
        sequence.Should().Be(1);
    }

    [Fact]
    public async Task GetNextSequenceAsync_MultipleCalls_ReturnsIncreasingSequence()
    {
		// Arrange
		Guid lotId = Guid.NewGuid();

		// Act
		long seq1 = await _generator.GetNextSequenceAsync(lotId);
		long seq2 = await _generator.GetNextSequenceAsync(lotId);
		long seq3 = await _generator.GetNextSequenceAsync(lotId);

        // Assert
        seq1.Should().Be(1);
        seq2.Should().Be(2);
        seq3.Should().Be(3);
    }

    #endregion

    #region Thread-Safety Tests

    [Fact]
    public async Task GetNextSequence_ConcurrentCalls_UniqueSequences()
    {
		// Arrange
		Guid lotId = Guid.NewGuid();
        const int concurrentCalls = 100;

		// Act
		List<Task<long>> tasks = Enumerable.Range(0, concurrentCalls)
            .Select(_ => Task.Run(() => _generator.GetNextSequence(lotId)))
            .ToList();

		long[] sequences = await Task.WhenAll(tasks);

        // Assert
        sequences.Should().OnlyHaveUniqueItems();
        sequences.Should().HaveCount(concurrentCalls);
        sequences.Min().Should().Be(1);
        sequences.Max().Should().Be(concurrentCalls);
    }

    [Fact]
    public async Task GetNextSequenceAsync_ConcurrentCalls_UniqueSequences()
    {
		// Arrange
		Guid lotId = Guid.NewGuid();
        const int concurrentCalls = 100;

		// Act
		List<Task<long>> tasks = Enumerable.Range(0, concurrentCalls)
            .Select(_ => _generator.GetNextSequenceAsync(lotId))
            .ToList();

		long[] sequences = await Task.WhenAll(tasks);

        // Assert
        sequences.Should().OnlyHaveUniqueItems();
        sequences.Should().HaveCount(concurrentCalls);
    }

    #endregion

    #region GetCurrentSequence Tests

    [Fact]
    public void GetCurrentSequence_NoSequenceGenerated_ReturnsZero()
    {
		// Arrange
		Guid lotId = Guid.NewGuid();

		// Act
		long current = _generator.GetCurrentSequence(lotId);

        // Assert
        current.Should().Be(0);
    }

    [Fact]
    public void GetCurrentSequence_AfterSequencesGenerated_ReturnsLastSequence()
    {
		// Arrange
		Guid lotId = Guid.NewGuid();
        _generator.GetNextSequence(lotId);
        _generator.GetNextSequence(lotId);
        _generator.GetNextSequence(lotId);

		// Act
		long current = _generator.GetCurrentSequence(lotId);

        // Assert
        current.Should().Be(3);
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_ClearsSequenceForLot()
    {
		// Arrange
		Guid lotId = Guid.NewGuid();
        _generator.GetNextSequence(lotId);
        _generator.GetNextSequence(lotId);

        // Act
        _generator.Reset(lotId);
		long afterReset = _generator.GetNextSequence(lotId);

        // Assert
        afterReset.Should().Be(1);
    }

    [Fact]
    public void ResetAll_ClearsAllSequences()
    {
		// Arrange
		Guid lotId1 = Guid.NewGuid();
		Guid lotId2 = Guid.NewGuid();
        _generator.GetNextSequence(lotId1);
        _generator.GetNextSequence(lotId2);

        // Act
        _generator.ResetAll();
		long lot1AfterReset = _generator.GetNextSequence(lotId1);
		long lot2AfterReset = _generator.GetNextSequence(lotId2);

        // Assert
        lot1AfterReset.Should().Be(1);
        lot2AfterReset.Should().Be(1);
    }

    #endregion
}