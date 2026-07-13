using AutoMapper;
using Domain.DTOs.Location;
using Domain.DTOs.Post;
using Domain.DTOs.User;
using Domain.DTOs.UserProfile;
using Domain.Entities;

namespace Infrastructure.Mapping;

/// <summary>
/// Базовые маппинги Entity ↔ DTO. Вычисляемые поля (счётчики, флаги текущего юзера,
/// имена/аватары из навигаций) заполняются в сервисах фич и здесь помечены Ignore.
/// </summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // ── Location ─────────────────────────────────────────────────────────
        CreateMap<Location, GetLocationDto>();
        CreateMap<AddLocationDto, Location>()
            .ForMember(d => d.Id, o => o.Ignore());
        CreateMap<UpdateLocationDto, Location>()
            .ForMember(d => d.Id, o => o.MapFrom(s => s.LocationId));

        // ── User ─────────────────────────────────────────────────────────────
        CreateMap<User, GetUserDto>();

        // ── UserProfile ──────────────────────────────────────────────────────
        CreateMap<UserProfile, GetUserProfileDto>()
            .ForMember(d => d.UserName, o => o.Ignore())
            .ForMember(d => d.FullName, o => o.Ignore())
            .ForMember(d => d.PostCount, o => o.Ignore())
            .ForMember(d => d.FollowersCount, o => o.Ignore())
            .ForMember(d => d.FollowingCount, o => o.Ignore())
            .ForMember(d => d.IsFollowing, o => o.Ignore());

        // ── Post ─────────────────────────────────────────────────────────────
        CreateMap<Post, GetPostDto>()
            .ForMember(d => d.Images, o => o.MapFrom(s => s.Images.Select(i => i.ImageName)))
            .ForMember(d => d.UserName, o => o.Ignore())
            .ForMember(d => d.UserImage, o => o.Ignore())
            .ForMember(d => d.LikeCount, o => o.Ignore())
            .ForMember(d => d.CommentCount, o => o.Ignore())
            .ForMember(d => d.ViewCount, o => o.Ignore())
            .ForMember(d => d.IsLiked, o => o.Ignore())
            .ForMember(d => d.IsFavorite, o => o.Ignore())
            .ForMember(d => d.MentionedUsers, o => o.Ignore());

        CreateMap<PostComment, GetPostCommentDto>()
            .ForMember(d => d.UserName, o => o.Ignore())
            .ForMember(d => d.UserImage, o => o.Ignore())
            .ForMember(d => d.MentionedUsers, o => o.Ignore());
    }
}
