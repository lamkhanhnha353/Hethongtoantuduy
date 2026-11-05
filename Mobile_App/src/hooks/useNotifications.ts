import {
  useInfiniteQuery, useMutation, useQuery, useQueryClient, type InfiniteData, type QueryKey,
} from '@tanstack/react-query';

import {
  notificationListQueryKey,
  notificationListRootQueryKey,
  notificationsRootQueryKey,
  notificationUnreadCountQueryKey,
} from '@/lib/queryKeys';
import {
  getNotifications,
  getUnreadCount,
  markAllNotificationsRead,
  markNotificationRead,
} from '@/services/notificationService';
import type {
  NotificationListResponseDto,
  NotificationType,
  NotificationUnreadCountResponseDto,
} from '@/types/notification';

const NOTIFICATION_PAGE_SIZE = 20;

export type NotificationFilters = {
  unreadOnly: boolean;
  type: NotificationType | null;
};

type NotificationListData = InfiniteData<NotificationListResponseDto, number>;
type NotificationListSnapshot = [QueryKey, NotificationListData | undefined][];

type ListMutationContext = {
  previousLists: NotificationListSnapshot;
  previousUnread: NotificationUnreadCountResponseDto | undefined;
};

export function useNotificationsQuery(filters: NotificationFilters, enabled = true) {
  return useInfiniteQuery({
    queryKey: notificationListQueryKey(filters),
    queryFn: ({ pageParam }) =>
      getNotifications({
        page: pageParam,
        pageSize: NOTIFICATION_PAGE_SIZE,
        unreadOnly: filters.unreadOnly,
        type: filters.type,
      }),
    initialPageParam: 1,
    getNextPageParam: (lastPage) => {
      const loadedItems = lastPage.page * lastPage.pageSize;
      return loadedItems < lastPage.total ? lastPage.page + 1 : undefined;
    },
    enabled,
    staleTime: 30_000,
  });
}
// Phiên bản cũ chưa có tính realtime
export function useNotificationUnreadCountQuery(enabled = true) {
  return useQuery<NotificationUnreadCountResponseDto>({
    queryKey: notificationUnreadCountQueryKey,
    queryFn: getUnreadCount,
    enabled,
    staleTime: 30_000,
    refetchInterval: enabled ? 60_000 : false,
  });
}

function markItemRead(data: NotificationListData, notificationId: number) {
  let didMark = false;

  const pages = data.pages.map((page) => {
    let markedInPage = false;

    const items = page.items.map((item) => {
      if (item.id !== notificationId || item.isRead) {
        return item;
      }

      markedInPage = true;
      return { ...item, isRead: true };
    });

    if (!markedInPage) {
      return page;
    }

    didMark = true;
    return {
      ...page,
      items,
      unreadCount: Math.max(0, page.unreadCount - 1),
    };
  });

  return {
    data: didMark ? { ...data, pages } : data,
    didMark,
  };
}

function markEveryItemRead(data: NotificationListData): NotificationListData {
  return {
    ...data,
    pages: data.pages.map((page) => ({
      ...page,
      unreadCount: 0,
      items: page.items.map((item) => (item.isRead ? item : { ...item, isRead: true })),
    })),
  };
}

function readSnapshots(
  queryClient: ReturnType<typeof useQueryClient>,
): ListMutationContext {
  return {
    previousLists: queryClient.getQueriesData<NotificationListData>({
      queryKey: notificationListRootQueryKey,
    }),
    previousUnread: queryClient.getQueryData<NotificationUnreadCountResponseDto>(
      notificationUnreadCountQueryKey,
    ),
  };
}

function restoreSnapshots(
  queryClient: ReturnType<typeof useQueryClient>,
  context: ListMutationContext | undefined,
) {
  context?.previousLists.forEach(([queryKey, data]) => {
    queryClient.setQueryData(queryKey, data);
  });

  if (context?.previousUnread) {
    queryClient.setQueryData(notificationUnreadCountQueryKey, context.previousUnread);
  }
}

export function useMarkNotificationReadMutation() {
  const queryClient = useQueryClient();

  return useMutation<void, unknown, number, ListMutationContext>({
    mutationFn: markNotificationRead,
    onMutate: async (notificationId) => {
      await queryClient.cancelQueries({ queryKey: notificationsRootQueryKey });

      const context = readSnapshots(queryClient);
      let didMark = false;

      queryClient.setQueriesData<NotificationListData>(
        { queryKey: notificationListRootQueryKey },
        (oldData) => {
          if (!oldData) {
            return oldData;
          }

          const result = markItemRead(oldData, notificationId);
          didMark = didMark || result.didMark;
          return result.data;
        },
      );

      if (didMark) {
        queryClient.setQueryData<NotificationUnreadCountResponseDto>(
          notificationUnreadCountQueryKey,
          (oldData) =>
            oldData
              ? { unreadCount: Math.max(0, oldData.unreadCount - 1) }
              : oldData,
        );
      }

      return context;
    },
    onError: (_error, _notificationId, context) => {
      restoreSnapshots(queryClient, context);
    },
    onSettled: () => {
      void queryClient.invalidateQueries({ queryKey: notificationsRootQueryKey });
    },
  });
}

export function useMarkAllNotificationsReadMutation() {
  const queryClient = useQueryClient();

  return useMutation<{ updated: number }, unknown, void, ListMutationContext>({
    mutationFn: markAllNotificationsRead,
    onMutate: async () => {
      await queryClient.cancelQueries({ queryKey: notificationsRootQueryKey });

      const context = readSnapshots(queryClient);

      queryClient.setQueriesData<NotificationListData>(
        { queryKey: notificationListRootQueryKey },
        (oldData) => (oldData ? markEveryItemRead(oldData) : oldData),
      );
      queryClient.setQueryData<NotificationUnreadCountResponseDto>(
        notificationUnreadCountQueryKey,
        (oldData) => (oldData ? { unreadCount: 0 } : oldData),
      );

      return context;
    },
    onError: (_error, _variables, context) => {
      restoreSnapshots(queryClient, context);
    },
    onSettled: () => {
      void queryClient.invalidateQueries({ queryKey: notificationsRootQueryKey });
    },
  });
}
