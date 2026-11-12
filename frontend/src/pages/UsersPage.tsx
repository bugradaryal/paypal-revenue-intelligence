import { useState, type FormEvent } from 'react'
import { Trash2 } from 'lucide-react'
import { PageHeader } from '@/components/layout/PageHeader'
import { Badge, Button, Card, CardHeader, Dialog, Field, Input, Select, Skeleton, useToast } from '@/components/ui'
import { DataTable, EmptyState, type Column } from '@/components/patterns'
import { useAdminUsers, useCreateAdminUser, useDeleteAdminUser } from '@/features/users/queries'
import { useAuth } from '@/features/auth/AuthProvider'
import { useI18n } from '@/i18n'
import { useErrorMessage } from '@/i18n/useErrorMessage'
import type { AdminUser, UserRole } from '@/services/types'

export function UsersPage() {
  const { t } = useI18n()
  const errorMessage = useErrorMessage()
  const { toast } = useToast()
  const { user: currentUser } = useAuth()
  const users = useAdminUsers()
  const createUser = useCreateAdminUser()
  const deleteUser = useDeleteAdminUser()
  const [userToDelete, setUserToDelete] = useState<AdminUser | null>(null)
  const [email, setEmail] = useState('')
  const [displayName, setDisplayName] = useState('')
  const [password, setPassword] = useState('')
  const [role, setRole] = useState<UserRole>('member')

  const columns: Column<AdminUser>[] = [
    { key: 'name', header: t('users.name'), cell: (user) => user.displayName },
    { key: 'email', header: t('users.email'), cell: (user) => user.email },
    {
      key: 'role',
      header: t('users.role'),
      cell: (user) => (
        <Badge tone={user.role === 'admin' ? 'brand' : 'neutral'}>
          {user.role === 'admin' ? t('users.roles.admin') : t('users.roles.member')}
        </Badge>
      ),
    },
    {
      key: 'actions',
      header: t('common.actions'),
      cell: (target) => (
        <Button
          variant="danger"
          size="sm"
          leadingIcon={<Trash2 className="size-4" />}
          disabled={target.id === currentUser?.id}
          aria-label={t('users.deleteAction', { email: target.email })}
          title={target.id === currentUser?.id ? t('users.cannotDeleteSelf') : undefined}
          onClick={() => setUserToDelete(target)}
        >
          {t('common.delete')}
        </Button>
      ),
    },
  ]

  function onSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    createUser.mutate(
      { email, displayName, password, role },
      {
        onSuccess: (user) => {
          setEmail('')
          setDisplayName('')
          setPassword('')
          setRole('member')
          toast({ tone: 'success', title: t('users.created', { email: user.email }) })
        },
      },
    )
  }

  function confirmDelete() {
    if (!userToDelete) return
    deleteUser.mutate(userToDelete.id, {
      onSuccess: () => {
        toast({ tone: 'success', title: t('users.deleted', { email: userToDelete.email }) })
        setUserToDelete(null)
      },
    })
  }

  return (
    <>
      <PageHeader title={t('users.title')} description={t('users.description')} />
      <div className="grid items-start gap-6 xl:grid-cols-[minmax(0,1fr)_minmax(20rem,0.75fr)]">
        <Card>
          <CardHeader title={t('users.listTitle')} description={t('users.listDescription')} />
          {users.isPending ? (
            <div className="grid gap-3">
              <Skeleton className="h-10 w-full" />
              <Skeleton className="h-10 w-full" />
              <Skeleton className="h-10 w-full" />
            </div>
          ) : users.isError ? (
            <EmptyState
              tone="danger"
              title={errorMessage(users.error)}
              className="py-4"
              action={<Button size="sm" variant="secondary" onClick={() => void users.refetch()}>{t('common.retry')}</Button>}
            />
          ) : (
            <DataTable
              caption={t('users.listTitle')}
              columns={columns}
              rows={users.data}
              getRowId={(user) => user.id}
              emptyTitle={t('users.empty')}
            />
          )}
        </Card>

        <Card>
          <CardHeader title={t('users.createTitle')} description={t('users.createDescription')} />
          <form onSubmit={onSubmit} className="grid gap-4">
            <Field label={t('users.name')} required>
              {({ id }) => (
                <Input
                  id={id}
                  autoComplete="name"
                  maxLength={100}
                  required
                  value={displayName}
                  onChange={(event) => setDisplayName(event.target.value)}
                />
              )}
            </Field>
            <Field label={t('users.email')} required>
              {({ id }) => (
                <Input
                  id={id}
                  type="email"
                  autoComplete="email"
                  maxLength={254}
                  required
                  value={email}
                  onChange={(event) => setEmail(event.target.value)}
                />
              )}
            </Field>
            <Field label={t('users.password')} hint={t('users.passwordHint')} required>
              {({ id }) => (
                <Input
                  id={id}
                  type="password"
                  autoComplete="new-password"
                  minLength={12}
                  maxLength={128}
                  required
                  value={password}
                  onChange={(event) => setPassword(event.target.value)}
                />
              )}
            </Field>
            <Field label={t('users.role')} required>
              {({ id }) => (
                <Select
                  id={id}
                  value={role}
                  onChange={(event) => setRole(event.target.value as UserRole)}
                  options={[
                    { value: 'member', label: t('users.roles.member') },
                    { value: 'admin', label: t('users.roles.admin') },
                  ]}
                />
              )}
            </Field>
            {createUser.isError && <p role="alert" className="text-sm text-danger">{errorMessage(createUser.error)}</p>}
            <div>
              <Button type="submit" loading={createUser.isPending}>
                {t('users.create')}
              </Button>
            </div>
          </form>
        </Card>
      </div>
      <Dialog
        open={userToDelete !== null}
        onClose={() => {
          if (!deleteUser.isPending) {
            setUserToDelete(null)
            deleteUser.reset()
          }
        }}
        title={t('users.deleteTitle')}
        description={userToDelete ? t('users.deleteDescription', { email: userToDelete.email }) : undefined}
        footer={
          <>
            <Button
              variant="ghost"
              disabled={deleteUser.isPending}
              onClick={() => {
                setUserToDelete(null)
                deleteUser.reset()
              }}
            >
              {t('common.cancel')}
            </Button>
            <Button variant="danger" loading={deleteUser.isPending} onClick={confirmDelete}>
              {t('users.confirmDelete')}
            </Button>
          </>
        }
      >
        {deleteUser.isError && <p role="alert" className="text-sm text-danger">{errorMessage(deleteUser.error)}</p>}
      </Dialog>
    </>
  )
}
