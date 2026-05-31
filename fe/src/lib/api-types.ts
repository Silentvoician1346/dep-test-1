export type AuthUser = {
  id: string;
  email: string;
  displayName: string;
  role: string;
  isActive: boolean;
};

export type BackendAuthResponse = {
  sessionId: string;
  expiresAt: string;
  user: AuthUser;
};

export type PagedResponse<T> = {
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
  items: T[];
};

export type Project = {
  id: string;
  ownerId: string;
  ownerEmail: string;
  name: string;
  status: string;
  createdAt: string;
  taskCount: number;
};

export type ProjectTask = {
  id: string;
  projectId: string;
  projectName: string;
  title: string;
  isDone: boolean;
  createdAt: string;
};

export type Announcement = {
  id: string;
  title: string;
  body: string;
  publishedAt: string;
};

export type DashboardProject = Project & {
  tasks: PagedResponse<ProjectTask>;
};

export type DashboardQuery = {
  projectsPage: number;
  projectsPageSize: number;
  tasksPage: number;
  tasksPageSize: number;
  announcementsPage: number;
  announcementsPageSize: number;
};

export type DashboardResponse = {
  user: AuthUser;
  query: DashboardQuery;
  projects: PagedResponse<DashboardProject>;
  announcements: PagedResponse<Announcement>;
};
