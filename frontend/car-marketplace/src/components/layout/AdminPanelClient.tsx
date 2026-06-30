"use client";

import { useEffect, useState } from "react"; 
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import {
  AdminUsersResponse,
  AdminUsersResponse as UserItem,
} from "@/Interface/AdminUsers";
import {
  AdminListingsResponse, 
} from "@/Interface/AdminListings"; 
import UpdateUserAdmin from "./UpdateUserAdmin";
import DeleteUserLayout from "./DeleteUserLayout";
import DeleteListCarLayout from "./DeleteListCarLayout";

export default function AdminPanelClient({ token }: {token : string}) {
  const [tab, setTab] = useState<"users" | "listings">("users");
  const [users, setUsers] = useState<UserItem[]>([]);
  const [listings, setListings] = useState<AdminListingsResponse[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchData = async () => {
      setLoading(true);
      setError(null);

      try {
        let url = "";

        if (tab === "users") {
          url =
            "http://localhost:5127/api/admin/users?pageNumber=1&pageSize=10";
        } else {
          url = "http://localhost:5127/api/admin/listings";
        }

        const res = await fetch(url, {
          headers: {
            Authorization: `Bearer ${token}`,
            Accept: "application/json",
          },
          cache: "no-store",
        });

        if (!res.ok) {
          const text = await res.text();
          throw new Error(text || `HTTP Error ${res.status}`);
        }

        const json = await res.json();

        if (tab === "users") {
          const data: AdminUsersResponse = json;
          setUsers(data.items);
        } else {
          const data: AdminListingsResponse = json;
          setListings(data.items);
        }
      } catch (err: any) {
        setError(err.message || "Something went wrong");
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, [tab, token]);

  return (
    <>
      <div className="w-full">
        {/* HEADER */}
        <div className="mt-8 mb-6 px-4">
          <h1 className="text-3xl font-extrabold">Admin Panel</h1>
          <p className="mt-1 text-md text-slate-600">
            Manage users and content.
          </p>
        </div>

        
        <Tabs value={tab} onValueChange={(value) => setTab(value as any)}>
          <TabsList className="mb-4 mx-4">
            <TabsTrigger value="users">Users</TabsTrigger>
            <TabsTrigger value="listings">Listings</TabsTrigger>
          </TabsList>
        </Tabs>

        
          
        {tab === "users" && (
          <div className="rounded-xl border bg-card mb-12 mx-4 overflow-hidden">
            <div className="mt-4 mb-6 px-4">
              <h2 className="text-md font-bold">User Management</h2>
              <p className="mt-1 text-sm text-slate-600">
                View and manage registered users.
              </p>
            </div>

            
            <div className="px-5 py-4">
              <Table className="w-full table-fixed">
                <TableHeader>
                  <TableRow>
                    <TableHead className="text-sm text-slate-500">
                      Name
                    </TableHead>
                    <TableHead className="text-sm text-slate-500 hidden md:table-cell">
                      Email
                    </TableHead>
                    <TableHead className="text-sm text-slate-500 hidden sm:table-cell">
                      Role
                    </TableHead>
                    <TableHead className="text-sm text-slate-500 text-right">
                      Actions
                    </TableHead>
                  </TableRow>
                </TableHeader>

                <TableBody>
                  {users.map((user) => (
                    <TableRow key={user.id}>
                      <TableCell className="font-medium truncate py-4">
                        {user.fullName}
                      </TableCell>

                      <TableCell className="hidden md:table-cell truncate max-w-50">
                        {user.email}
                      </TableCell>

                      <TableCell className="hidden sm:table-cell">
                        {user.role === "Admin" ? (
                          <span className="bg-blue-100 text-blue-700 px-2 py-1 rounded-md text-sm">
                            {user.role}
                          </span>
                        ) : (
                          user.role
                        )}
                      </TableCell>

                      <TableCell className="text-right">
                        <div className="flex items-center justify-end gap-2">
                          <UpdateUserAdmin user={user} 
                                          onUserUpdated={(updatedUser) => {setUsers((prev) => prev.map((u) => (u.id === updatedUser.id ? updatedUser : u)))
                          }}/>
                          <DeleteUserLayout userId={user.id} onUserDeleted={(id) => {
    setUsers((prev) => prev.filter((u) => u.id !== id))
  }}/>
                        </div>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          </div>
        )}

       
        {tab === "listings" && (
          <div className="rounded-xl border bg-card mb-12 mx-4">
            <div className="mt-4 mb-6 px-4">
              <h2 className="text-md font-bold">All Listings</h2>
              <p className="mt-1 text-sm text-slate-600">
                Manage all car listings on the platform.
              </p>
            </div>

            <div className="px-4">
              <Table className="w-full table-fixed">
                <TableHeader>
                  <TableRow>
                    <TableHead className="text-sm text-slate-500">
                      Car
                    </TableHead>
                    <TableHead className="text-sm text-slate-500">
                      Owner
                    </TableHead>
                    <TableHead className="text-sm text-slate-500">
                      Price
                    </TableHead>
                    <TableHead className="text-sm text-slate-500 text-right">
                      Delete
                    </TableHead>
                  </TableRow>
                </TableHeader>

                <TableBody>
                  {listings.map((list) => (
                    <TableRow key={list.id}>
                      <TableCell className="py-4 font-medium truncate">
                        {list.title}
                      </TableCell>
                      <TableCell className="truncate">
                        {list.ownerFullName}
                      </TableCell>
                      <TableCell>{list.price}</TableCell>

                      <TableCell className="text-right">
                        <div className="flex items-center justify-end gap-3">
                          <DeleteListCarLayout listId={list.id}  onDeleted={(id) => {
    setListings((prev) => prev.filter((l) => l.id !== id))
  }}/>
                        </div>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </div>
          </div>
        )}
      </div>
    </>
  );
}