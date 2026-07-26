using SigortaPro.Application.Common.Interfaces;

namespace SigortaPro.Application.Features.Notifications.Commands.MarkAllNotificationsAsRead;

// Oturum sahibinin tüm okunmamış bildirimlerini okundu işaretler (idempotent).
public sealed record MarkAllNotificationsAsReadCommand : ICommand;
