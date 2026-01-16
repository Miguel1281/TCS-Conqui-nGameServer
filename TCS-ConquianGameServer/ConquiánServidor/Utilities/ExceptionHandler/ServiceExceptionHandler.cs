using ConquiánServidor.BusinessLogic.Exceptions;
using ConquiánServidor.Contracts.DataContracts;
using NLog;
using System;
using System.Data.Entity.Core;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Net.Mail;
using System.ServiceModel;

namespace ConquiánServidor.Utilities.ExceptionHandler
{
    public class ServiceExceptionHandler : IServiceExceptionHandler
    {
        private readonly Logger logger = LogManager.GetCurrentClassLogger();

        private const int SQL_ERROR_UNIQUE_CONSTRAINT = 2627;
        private const int SQL_ERROR_DUPLICATE_KEY = 2601;
        private const int SQL_ERROR_FOREIGN_KEY = 547;
        private const int SQL_ERROR_TIMEOUT = -2;
        private const int SQL_ERROR_DEADLOCK = 1205;
        private const int SQL_ERROR_CONNECTION_FAILED = 53;

        public FaultException<ServiceFaultDto> HandleException(Exception exception, string operationContext)
        {
            if (exception is BusinessLogicException blEx)
            {
                logger.Warn(blEx, "Business logic error in {0}: {1}", operationContext, blEx.Message);
                return CreateFault(blEx.ErrorType);
            }

            if (exception is GuestInviteUsedException)
            {
                logger.Warn(exception, "Guest invite already used in {0}", operationContext);
                return CreateFault(ServiceErrorType.GuestInviteUsed);
            }

            if (exception is RegisteredUserAsGuestException)
            {
                logger.Warn(exception, "Registered user tried to join as guest in {0}", operationContext);
                return CreateFault(ServiceErrorType.RegisteredUserAsGuest);
            }

            if (exception is DbUpdateException dbUpdateEx)
            {
                return HandleDbUpdateException(dbUpdateEx, operationContext);
            }

            if (exception is EntityException)
            {
                logger.Error(exception, "Entity Framework error in {0}", operationContext);
                return CreateFault(ServiceErrorType.DatabaseError);
            }

            if (exception is SqlException sqlEx)
            {
                return HandleSqlException(sqlEx, operationContext);
            }

            if (exception is SmtpException smtpEx)
            {
                logger.Error(smtpEx, "SMTP error in {0}. StatusCode: {1}", operationContext, smtpEx.StatusCode);
                return CreateFault(ServiceErrorType.CommunicationError);
            }

            if (exception is FormatException)
            {
                logger.Warn(exception, "Format error in {0}", operationContext);
                return CreateFault(ServiceErrorType.InvalidEmailFormat);
            }

            if (exception is CommunicationException)
            {
                logger.Error(exception, "Communication error in {0}", operationContext);
                return CreateFault(ServiceErrorType.CommunicationError);
            }

            if (exception is TimeoutException)
            {
                logger.Error(exception, "Timeout in {0}", operationContext);
                return CreateFault(ServiceErrorType.CommunicationError);
            }

            if (exception is ArgumentNullException argNullEx)
            {
                logger.Warn(argNullEx, "Null argument in {0}: {1}", operationContext, argNullEx.ParamName);
                return CreateFault(ServiceErrorType.ValidationFailed);
            }

            if (exception is ArgumentException)
            {
                logger.Warn(exception, "Invalid argument in {0}", operationContext);
                return CreateFault(ServiceErrorType.ValidationFailed);
            }

            if (exception is InvalidOperationException)
            {
                logger.Error(exception, "Invalid operation in {0}", operationContext);
                return CreateFault(ServiceErrorType.OperationFailed);
            }

            logger.Fatal(exception, "Unhandled exception in {0}. Type: {1}", operationContext,
                exception.GetType().Name);
            return CreateFault(ServiceErrorType.ServerInternalError);
        }

        private FaultException<ServiceFaultDto> HandleDbUpdateException(DbUpdateException ex, string context)
        {
            var sqlEx = ex.InnerException?.InnerException as SqlException;

            if (sqlEx != null)
            {
                if (sqlEx.Number == SQL_ERROR_UNIQUE_CONSTRAINT || sqlEx.Number == SQL_ERROR_DUPLICATE_KEY)
                {
                    logger.Warn(ex, "Duplicate entry in {0}", context);
                    return CreateFault(ServiceErrorType.DuplicateRecord);
                }

                if (sqlEx.Number == SQL_ERROR_FOREIGN_KEY)
                {
                    logger.Error(ex, "Foreign key violation in {0}", context);
                    return CreateFault(ServiceErrorType.OperationFailed);
                }

                if (sqlEx.Number == SQL_ERROR_DEADLOCK)
                {
                    logger.Warn(ex, "Database deadlock in {0}", context);
                    return CreateFault(ServiceErrorType.DatabaseError);
                }
            }

            logger.Error(ex, "Database update failed in {0}", context);
            return CreateFault(ServiceErrorType.DatabaseError);
        }

        private FaultException<ServiceFaultDto> HandleSqlException(SqlException ex, string context)
        {
            if (ex.Number == SQL_ERROR_TIMEOUT)
            {
                logger.Error(ex, "SQL timeout in {0}", context);
                return CreateFault(ServiceErrorType.DatabaseError);
            }

            if (ex.Number == SQL_ERROR_CONNECTION_FAILED)
            {
                logger.Error(ex, "SQL connection failed in {0}", context);
                return CreateFault(ServiceErrorType.DatabaseError);
            }

            if (ex.Number == SQL_ERROR_DEADLOCK)
            {
                logger.Warn(ex, "SQL deadlock in {0}", context);
                return CreateFault(ServiceErrorType.DatabaseError);
            }

            logger.Error(ex, "SQL exception {0} in {1}", ex.Number, context);
            return CreateFault(ServiceErrorType.DatabaseError);
        }

        private FaultException<ServiceFaultDto> CreateFault(ServiceErrorType type)
        {
            var fault = new ServiceFaultDto(type, type.ToString(), null);
            return new FaultException<ServiceFaultDto>(fault, new FaultReason(type.ToString()));
        }
    }
}