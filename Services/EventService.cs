using Dapper;
using Microsoft.AspNetCore.Connections;
using Microsoft.Data.SqlClient;
using System.Data;
using Training.DTOs.Training;
using Training.Models.DTO;
using Training.Models.DTO.Common;
using Training.Models.DTO.Event;
using Training.Services.IServices;

namespace Training.Services
{
    public class EventService : IEventService
    {

        private readonly string _connectionString;
        private readonly ILogger<EventService> _logger;

        public EventService(
            IConfiguration configuration,
            ILogger<EventService> logger)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Connection string 'DefaultConnection' was not found.");

            _logger = logger;
        }

        public async Task<List<TrainingEventDto>> GetTrainingEventsAsync()
        {
            try
            {
                await using var connection =
                    new SqlConnection(_connectionString);

                await connection.OpenAsync();

                var result = await connection.QueryAsync<TrainingEventDto>(
                    "USP_Event_Training");

                return result.AsList();
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving training events.");

                throw;
            }
        }

        public async Task<DataTableResponseDto<TrainingEventDto>>
        GetTrainingEventsAsync(
            EventFilterDto filter,
            string username)
        {
            var request = filter.DataTable;

            var searchTerm = string.IsNullOrWhiteSpace(
                request.SearchValue)
                ? null
                : request.SearchValue.Trim();

            var coCode = string.IsNullOrWhiteSpace(
                filter.CoCode)
                ? null
                : filter.CoCode.Trim();

            var abrv = string.IsNullOrWhiteSpace(
                filter.Abrv)
                ? null
                : filter.Abrv.Trim();

            var status = string.IsNullOrWhiteSpace(
                filter.Status)
                ? null
                : filter.Status.Trim();

            var orderDirection =
                string.Equals(
                    request.OrderDirection,
                    "asc",
                    StringComparison.OrdinalIgnoreCase)
                    ? "ASC"
                    : "DESC";

            try
            {
                await using var connection =
                    new SqlConnection(_connectionString);

                var parameters = new DynamicParameters();

                parameters.Add("@SearchTerm", searchTerm);
                parameters.Add("@CoCode", coCode);
                parameters.Add("@ABRV", abrv);
                parameters.Add("@Status", status);

                parameters.Add(
                    "@Start",
                    Math.Max(request.Start, 0));

                parameters.Add(
                    "@Length",
                    request.Length <= 0
                        ? 10
                        : Math.Min(request.Length, 500));

                parameters.Add(
                    "@OrderColumn",
                    request.OrderColumn);

                parameters.Add(
                    "@OrderDirection",
                    orderDirection);

                parameters.Add(
                    "@Username",
                    username.Trim(),
                    DbType.AnsiString,
                    size: 50);

                using var multi = await connection.QueryMultipleAsync(
                    "dbo.usp_TrainingEvent_GetList",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                var totalRecords =
                    await multi.ReadSingleAsync<long>();

                var filteredRecords =
                    await multi.ReadSingleAsync<long>();

                var data =
                    (await multi.ReadAsync<TrainingEventDto>())
                    .AsList();

                return new DataTableResponseDto<TrainingEventDto>
                {
                    Draw = request.Draw,
                    RecordsTotal = Convert.ToInt32(totalRecords),
                    RecordsFiltered = Convert.ToInt32(filteredRecords),
                    Data = data
                };
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "Error retrieving training events. " +
                    "SearchTerm: {SearchTerm}, " +
                    "CoCode: {CoCode}, " +
                    "ABRV: {ABRV}, " +
                    "Status: {Status}",
                    searchTerm,
                    coCode,
                    abrv,
                    status);

                throw;
            }
        }

        public async Task<TrainingEventListResultDto> GetTrainingEventsAsync(
            TrainingEventFilterDto filter, string username)
        {
            using var connection =
                new SqlConnection(_connectionString);

            var parameters = new DynamicParameters();

            parameters.Add("@SearchTerm", filter.SearchTerm);
            parameters.Add("@CoCode", filter.CoCode);
            parameters.Add("@ABRV", filter.Abrv);
            parameters.Add("@Status", filter.Status);

            parameters.Add("@Start", filter.Start);
            parameters.Add("@Length", filter.Length);

            parameters.Add(
                "@OrderColumn",
                filter.OrderColumn ?? "EventStartDate");

            parameters.Add(
                "@OrderDirection",
                filter.OrderDirection ?? "ASC");

            parameters.Add(
    "@Username",
    username.Trim(),
    DbType.AnsiString,
    size: 50);

            using var multi =
                await connection.QueryMultipleAsync(
                    "dbo.usp_TrainingEvent_GetList",
                    parameters,
                    commandType: CommandType.StoredProcedure);

            var totalRecords =
                await multi.ReadSingleAsync<long>();

            var filteredRecords =
                await multi.ReadSingleAsync<long>();

            var data =
                (await multi.ReadAsync<TrainingEventDto>())
                .ToList();

            return new TrainingEventListResultDto
            {
                TotalRecords = totalRecords,
                FilteredRecords = filteredRecords,
                Data = data
            };
        }

        //Create Event
        public async Task<TrainingEventCreateResultDto> CreateAsync(
            TrainingEventCreateDto request,
            string createdBy,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(createdBy))
            {
                throw new ArgumentException(
                    "Created by is required.",
                    nameof(createdBy));
            }

            var participants =
                request.Participants ?? [];

            if (participants.Count == 0)
            {
                throw new ArgumentException(
                    "At least one participant is required.",
                    nameof(request));
            }

            /*
             * Build SQL Server Table-Valued Parameter.
             */
            var participantTable =
                BuildParticipantTable(participants);

            var parameters =
                new DynamicParameters();

            parameters.Add(
                "@CourseId",
                request.CourseId,
                DbType.Int32);

            parameters.Add(
                "@TrainingCategoryId",
                request.TrainingCategoryId,
                DbType.Int32);

            parameters.Add(
                "@TrainingTitle",
                request.TrainingTitle,
                DbType.String);

            parameters.Add(
                "@TrainingDescription",
                request.TrainingDescription,
                DbType.String);

            parameters.Add(
                "@TrainingObjective",
                request.TrainingObjective,
                DbType.String);

            parameters.Add(
                "@CoCode",
                request.CoCode,
                DbType.String);

            parameters.Add(
                "@ABRV",
                request.ABRV,
                DbType.String);

            parameters.Add(
                "@DeptName",
                request.DeptName,
                DbType.String);

            parameters.Add(
                "@EventType",
                request.EventType,
                DbType.AnsiStringFixedLength);

            parameters.Add(
                "@TrainerId",
                request.TrainerId,
                DbType.AnsiString);

            parameters.Add(
                "@TrainerName",
                request.TrainerName,
                DbType.String);

            parameters.Add(
                "@Budget",
                request.Budget,
                DbType.Decimal);

            parameters.Add(
                "@Venue",
                request.Venue,
                DbType.String);

            parameters.Add(
                "@EventStartDate",
                request.EventStartDate,
                DbType.DateTime2);

            parameters.Add(
                "@EventEndDate",
                request.EventEndDate,
                DbType.DateTime2);

            parameters.Add(
                "@SessionCount",
                request.SessionCount,
                DbType.Int32);

            parameters.Add(
                "@DurationHours",
                request.DurationHours,
                DbType.Decimal);

            parameters.Add(
                "@CreatedBy",
                createdBy,
                DbType.String);

            parameters.Add(
                "@Participants",
                participantTable.AsTableValuedParameter(
                    "dbo.TrainingEventParticipantType"));

            await using var connection =
                new SqlConnection(_connectionString);

            await connection.OpenAsync(
                cancellationToken);

            var command =
                new CommandDefinition(
                    commandText:
                        "dbo.usp_TrainingEvent_Create",
                    parameters: parameters,
                    commandType:
                        CommandType.StoredProcedure,
                    cancellationToken:
                        cancellationToken);

            try
            {
                var result =
                    await connection.QuerySingleAsync<
                        TrainingEventCreateResultDto>(
                        command);

                return result;
            }
            catch (SqlException ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to create training event for user {CreatedBy}.",
                    createdBy);

                throw;
            }
        }

        
        private static DataTable BuildParticipantTable(
            IEnumerable<TrainingEventParticipantDto> participants)
        {
            var table = new DataTable();

            table.Columns.Add(
                "EmployeeCode",
                typeof(string));

            table.Columns.Add(
                "Name",
                typeof(string));

            table.Columns.Add(
                "ABRV",
                typeof(string));

            table.Columns.Add(
                "DeptName",
                typeof(string));

            foreach (var participant in participants)
            {
                if (string.IsNullOrWhiteSpace(
                    participant.EmployeeCode))
                {
                    throw new ArgumentException(
                        "Participant employee code is required.");
                }

                if (string.IsNullOrWhiteSpace(
                    participant.Name))
                {
                    throw new ArgumentException(
                        "Participant name is required.");
                }

                table.Rows.Add(
                    participant.EmployeeCode.Trim(),
                    participant.Name.Trim(),
                    participant.ABRV?.Trim() ?? (object)DBNull.Value,
                    participant.DeptName?.Trim() ?? (object)DBNull.Value);
            }

            return table;
        }

        public async Task<TrainingEventDetailDto?> GetDetailAsync(
            long trainingEventId,
            string username,
            CancellationToken cancellationToken)
                {
                    if (trainingEventId <= 0)
                    {
                        throw new ArgumentException(
                            "Invalid training event identifier.",
                            nameof(trainingEventId));
                    }

                    if (string.IsNullOrWhiteSpace(username))
                    {
                        throw new ArgumentException(
                            "Username is required.",
                            nameof(username));
                    }

                    const string sql = "dbo.usp_TrainingEvent_GetDetail";

                    using var connection = new SqlConnection(_connectionString);

                    //using var connection = _connectionFactory.CreateConnection();

                    var command = new CommandDefinition(
                        sql,
                        new
                        {
                            TrainingEventId = trainingEventId,
                            Username = username
                        },
                        commandType: CommandType.StoredProcedure,
                        cancellationToken: cancellationToken);

                    using var multi = await connection.QueryMultipleAsync(command);

                    var eventDetail =
                        await multi.ReadFirstOrDefaultAsync<TrainingEventDetailDto>();

                    if (eventDetail is null)
                    {
                        return null;
                    }

                    var participants =
                        (await multi.ReadAsync<TrainingEventParticipantDto>())
                        .ToList();

                    eventDetail.Participants = participants;

            eventDetail.AvailableAction = GetAvailableAction(
                    eventDetail.Status,
                    eventDetail.CanAction);

            return eventDetail;
                }

        public async Task<TrainingEventDetailDto?> GetByIdAsync(
            long trainingEventId,
            CancellationToken cancellationToken)
        {
            if (trainingEventId <= 0)
            {
                throw new ArgumentException(
                    "Invalid training event ID.",
                    nameof(trainingEventId));
            }

            await using var connection =
                new SqlConnection(_connectionString);

            await connection.OpenAsync(cancellationToken);

            var command = new CommandDefinition(
                "dbo.usp_TrainingEvent_GetById",
                new
                {
                    TrainingEventId = trainingEventId
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken);

            using var multi =
                await connection.QueryMultipleAsync(command);

            var eventData =
                await multi.ReadFirstOrDefaultAsync<TrainingEventDetailDto>();

            if (eventData is null)
            {
                return null;
            }

            var participants =
                (await multi.ReadAsync<TrainingEventParticipantDto>())
                .ToList();

            eventData.Participants = participants;

            return eventData;
        }


        /// <inheritdoc />
        public async Task DeleteAsync(
            long trainingEventId,
            CancellationToken cancellationToken = default)
        {
            if (trainingEventId <= 0)
            {
                throw new ArgumentException(
                    "Invalid training event ID.",
                    nameof(trainingEventId));
            }

            await using var connection =
                new SqlConnection(_connectionString);

            var command = new CommandDefinition(
                commandText: "dbo.USP_TrainingEvent_Delete",
                parameters: new
                {
                    TrainingEventId = trainingEventId
                },
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken);

            await connection.ExecuteAsync(command);
        }

        /// <summary>
        /// Determines the workflow action available to the current user.
        /// </summary>
        /// <param name="status">Current training event status.</param>
        /// <param name="canAction">Indicates whether the user is allowed to perform an action.</param>
        /// <returns>The available UI action, or null when no action is available.</returns>
        private static string? GetAvailableAction(
            string? status,
            bool canAction)
        {
            if (!canAction || string.IsNullOrWhiteSpace(status))
            {
                return null;
            }

            if (status.Equals(
                    "Draft",
                    StringComparison.OrdinalIgnoreCase)
                ||
                status.Equals(
                    "Rejected",
                    StringComparison.OrdinalIgnoreCase))
            {
                return "Post";
            }

            if (status.StartsWith(
                    "Pending",
                    StringComparison.OrdinalIgnoreCase))
            {
                return "ApproveReject";
            }

            return null;
        }




    }
}
